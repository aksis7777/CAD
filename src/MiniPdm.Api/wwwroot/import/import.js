(() => {
  "use strict";

  const maxFiles = 1000;
  const maxFileBytes = 8 * 1024 * 1024;
  const maxTotalBytes = 64 * 1024 * 1024;
  const maxRequestBytes = 64 * 1024 * 1024;
  const encoder = new TextEncoder();
  const acceptedExtensions = new Set([".a3d", ".m3d"]);

  const folderInput = document.querySelector("#folder-input");
  const filesInput = document.querySelector("#files-input");
  const folderPickerButton = document.querySelector("#folder-picker-button");
  const filesPickerButton = document.querySelector("#files-picker-button");
  const pickers = document.querySelector("#pickers");
  const uploadButton = document.querySelector("#upload-button");
  const clearButton = document.querySelector("#clear-button");
  const selection = document.querySelector("#selection");
  const statusCard = document.querySelector("#status-card");
  const statusText = document.querySelector("#status-text");
  const summary = document.querySelector("#report-summary");
  const reportTable = document.querySelector("#report-table-wrap");
  const reportRows = document.querySelector("#report-rows");
  const unknownActions = document.querySelector("#unknown-actions");
  const retryButton = document.querySelector("#retry-button");
  const checkButton = document.querySelector("#check-button");
  const abandonButton = document.querySelector("#abandon-button");

  let selectedFiles = [];
  let selectedBytes = 0;
  let importId = null;
  let requestInFlight = false;
  let outcomeUnknown = false;

  function setStatus(message, kind) {
    statusCard.hidden = false;
    statusCard.dataset.kind = kind || "";
    statusText.textContent = message;
  }

  function clearReport() {
    summary.hidden = true;
    summary.replaceChildren();
    reportTable.hidden = true;
    reportRows.replaceChildren();
  }

  function updateControls() {
    const locked = requestInFlight || outcomeUnknown;
    pickers.hidden = locked;
    uploadButton.hidden = locked;
    uploadButton.disabled = selectedFiles.length === 0 || requestInFlight || outcomeUnknown;
    clearButton.hidden = selectedFiles.length === 0 || locked;
    retryButton.disabled = requestInFlight;
    checkButton.disabled = requestInFlight;
    abandonButton.disabled = requestInFlight;
  }

  function describeSelection() {
    if (selectedFiles.length === 0) {
      selection.textContent = "Папка ещё не выбрана.";
      selection.className = "selection";
      updateControls();
      return;
    }

    const size = formatBytes(selectedBytes);
    selection.className = "selection has-files";
    selection.textContent = `Файлов: ${selectedFiles.length} · размер: ${size}\nБудут отправлены только файлы .a3d и .m3d.`;
    updateControls();
  }

  function formatBytes(bytes) {
    if (bytes < 1024) return `${bytes} Б`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} КиБ`;
    return `${(bytes / (1024 * 1024)).toFixed(1)} МиБ`;
  }

  function basename(path) {
    return path.replaceAll("\\", "/").split("/").pop() || "";
  }

  function extension(name) {
    const dot = name.lastIndexOf(".");
    return dot < 0 ? "" : name.slice(dot).toLowerCase();
  }

  function setSelectionError(message) {
    selectedFiles = [];
    selectedBytes = 0;
    selection.textContent = message;
    selection.className = "selection has-error";
    clearReport();
    statusCard.hidden = true;
    unknownActions.hidden = true;
    updateControls();
  }

  function acceptSelection(fileList) {
    if (outcomeUnknown || requestInFlight) return;
    const files = Array.from(fileList || []).filter(file => acceptedExtensions.has(extension(file.name)));
    folderInput.value = "";
    filesInput.value = "";
    clearReport();
    importId = null;
    outcomeUnknown = false;
    statusCard.hidden = true;
    unknownActions.hidden = true;

    if (files.length === 0) {
      setSelectionError("В выбранном наборе не найдены файлы .a3d или .m3d.");
      return;
    }
    if (files.length > maxFiles) {
      setSelectionError(`Найдено ${files.length} подходящих файлов. За один раз можно загрузить не более ${maxFiles}.`);
      return;
    }

    const names = new Set();
    let total = 0;
    let estimatedOverhead = 2048;
    for (const file of files) {
      const name = basename(file.name);
      if (names.has(name)) {
        setSelectionError(`Имя файла «${name}» встречается больше одного раза. Сервер связывает детали по имени файла, поэтому выберите набор без одинаковых имён.`);
        return;
      }
      names.add(name);
      estimatedOverhead += 2048 + encoder.encode(name).length * 4;
      if (file.size > maxFileBytes) {
        setSelectionError(`Файл «${name}» занимает ${formatBytes(file.size)}. Максимальный размер одного файла — 8 МиБ.`);
        return;
      }
      total += file.size;
      if (total > maxTotalBytes) {
        setSelectionError("Общий размер выбранных CAD-файлов превышает 64 МиБ.");
        return;
      }
    }

    if (total + estimatedOverhead > maxRequestBytes) {
      setSelectionError(`Хотя сами файлы меньше лимита, общий размер запроса вместе со служебными данными превысит 64 МиБ. Уменьшите размер выбранного набора.`);
      return;
    }

    selectedFiles = files;
    selectedBytes = total;
    describeSelection();
  }

  function addDetail(list, value) {
    if (typeof value === "string" && value.trim()) list.push(value.trim());
  }

  function renderReport(report) {
    const files = Array.isArray(report?.files) ? report.files : [];
    const accepted = Number.isInteger(report?.acceptedCount) ? report.acceptedCount : files.filter(row => row.status === 0).length;
    const rejected = Number.isInteger(report?.rejectedCount) ? report.rejectedCount : files.filter(row => row.status === 1).length;
    const warnings = Number.isInteger(report?.warningCount)
      ? report.warningCount
      : files.filter(row => row.status === 0 && Array.isArray(row.warnings) && row.warnings.length > 0).length;

    summary.replaceChildren();
    const acceptedBadge = document.createElement("span");
    acceptedBadge.className = "accepted";
    acceptedBadge.textContent = `Принято: ${accepted}`;
    const rejectedBadge = document.createElement("span");
    rejectedBadge.className = "rejected";
    rejectedBadge.textContent = `Отклонено: ${rejected}`;
    const warningsBadge = document.createElement("span");
    warningsBadge.className = "warnings";
    warningsBadge.textContent = `Предупреждений: ${warnings}`;
    summary.append(acceptedBadge, rejectedBadge, warningsBadge);
    summary.hidden = false;

    reportRows.replaceChildren();
    for (const file of files) {
      const row = document.createElement("tr");
      const fileName = document.createElement("td");
      fileName.textContent = file?.fileName || "(имя не указано)";
      const result = document.createElement("td");
      const badge = document.createElement("span");
      const status = Number(file?.status);
      badge.className = `badge ${status === 0 ? "accepted" : "rejected"}`;
      badge.textContent = status === 0 ? "Принят" : "Отклонён";
      result.append(badge);
      const details = document.createElement("td");
      details.className = "details";
      const messages = [];
      addDetail(messages, file?.reason);
      const actions = ["Создан", "Обновлён", "Создана новая версия", "Без изменений"];
      if (Number.isInteger(file?.action) && actions[file.action]) messages.push(actions[file.action]);
      if (Array.isArray(file?.warnings)) for (const warning of file.warnings) addDetail(messages, warning);
      details.textContent = messages.join("\n") || "—";
      row.append(fileName, result, details);
      reportRows.append(row);
    }
    reportTable.hidden = files.length === 0;
    if (accepted > 0 && rejected === 0) setStatus("Импорт завершён. Вернитесь в настольное приложение и нажмите «Найти», чтобы обновить список объектов.", "success");
    else if (accepted > 0) setStatus("Импорт завершён с отклонёнными файлами. Проверьте отчёт. Затем вернитесь в настольное приложение и нажмите «Найти», чтобы обновить список объектов.", "warning");
    else setStatus("Импорт завершён, но сервер не принял ни одного файла. Проверьте отчёт.", "error");
  }

  async function readError(response) {
    try {
      const payload = await response.json();
      if (typeof payload === "string") return payload;
      if (typeof payload?.error === "string") return payload.error;
      if (typeof payload?.title === "string") return payload.title;
    } catch { /* The server may return a plain response. */ }
    return `Сервер ответил HTTP ${response.status}.`;
  }

  function showUnknown(message) {
    outcomeUnknown = true;
    unknownActions.hidden = false;
    clearReport();
    setStatus(`${message} Результат операции пока не подтверждён. Файлы сохранены в этой вкладке; повтор будет отправлен с тем же ID.`, "warning");
    updateControls();
  }

  async function submitImport(isRetry = false) {
    if (requestInFlight || (outcomeUnknown && !isRetry) || selectedFiles.length === 0) return;
    if (!importId) importId = crypto.randomUUID();
    requestInFlight = true;
    unknownActions.hidden = true;
    clearReport();
    setStatus("Передаём файлы и выполняем импорт. Не закрывайте эту вкладку.", "working");
    updateControls();

    const body = new FormData();
    for (const file of selectedFiles) body.append("files", file, basename(file.name));

    try {
      const response = await fetch(`/api/imports/${encodeURIComponent(importId)}`, {
        method: "POST",
        body,
        headers: { "Accept": "application/json" }
      });
      if (response.ok) {
        const report = await response.json();
        outcomeUnknown = false;
        unknownActions.hidden = true;
        renderReport(report);
      } else if (response.status >= 500 || response.status === 408 || response.status === 429) {
        showUnknown(`Сервер ответил HTTP ${response.status}.`);
      } else {
        outcomeUnknown = false;
        setStatus(await readError(response), "error");
      }
    } catch {
      showUnknown("Не удалось получить ответ от сервера.");
    } finally {
      requestInFlight = false;
      updateControls();
    }
  }

  async function checkReport() {
    if (requestInFlight || !importId) return;
    requestInFlight = true;
    setStatus("Проверяем сохранённый отчёт на сервере…", "working");
    updateControls();
    try {
      const response = await fetch(`/api/imports/${encodeURIComponent(importId)}`, { headers: { "Accept": "application/json" } });
      if (response.ok) {
        outcomeUnknown = false;
        unknownActions.hidden = true;
        renderReport(await response.json());
      } else if (response.status === 404) {
        outcomeUnknown = true;
        unknownActions.hidden = false;
        setStatus("Сохранённого отчёта пока нет. Можно повторить загрузку с тем же ID.", "warning");
      } else {
        const message = await readError(response);
        outcomeUnknown = true;
        unknownActions.hidden = false;
        setStatus(`${message} Результат операции остаётся неподтверждённым.`, "warning");
      }
    } catch {
      outcomeUnknown = true;
      unknownActions.hidden = false;
      setStatus("Не удалось проверить отчёт. Идентификатор и выбранные файлы сохранены в этой вкладке.", "warning");
    } finally {
      requestInFlight = false;
      updateControls();
    }
  }

  function abandonAndStartNew() {
    if (!outcomeUnknown || requestInFlight) return;
    const confirmed = window.confirm("Сервер мог уже завершить предыдущий импорт. Сохранённые объекты останутся в базе. Начать отдельную операцию с новым ID?");
    if (!confirmed) return;
    importId = null;
    outcomeUnknown = false;
    unknownActions.hidden = true;
    statusCard.hidden = true;
    clearReport();
    selectedFiles = [];
    selectedBytes = 0;
    folderInput.value = "";
    filesInput.value = "";
    describeSelection();
  }

  folderPickerButton.addEventListener("click", () => folderInput.click());
  filesPickerButton.addEventListener("click", () => filesInput.click());
  folderInput.addEventListener("change", event => acceptSelection(event.target.files));
  filesInput.addEventListener("change", event => acceptSelection(event.target.files));
  uploadButton.addEventListener("click", submitImport);
  clearButton.addEventListener("click", () => {
    selectedFiles = [];
    selectedBytes = 0;
    importId = null;
    clearReport();
    statusCard.hidden = true;
    unknownActions.hidden = true;
    folderInput.value = "";
    filesInput.value = "";
    describeSelection();
  });
  retryButton.addEventListener("click", () => submitImport(true));
  checkButton.addEventListener("click", checkReport);
  abandonButton.addEventListener("click", abandonAndStartNew);
  updateControls();
})();
