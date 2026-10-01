using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using MiniPdm.Contracts.Modules.Calculations.DtoModels;
using MiniPdm.Contracts.Modules.Composition.DtoModels;
using MiniPdm.Contracts.Modules.Objects.DtoModels;
using MiniPdm.Desktop.ViewModels;

namespace MiniPdm.Desktop.Modules.Composition.ViewModels;

public sealed class CompositionWorkspaceViewModel : INotifyPropertyChanged
{
    private readonly Action<Guid> _navigate;
    private bool _canEdit;
    private TreeOccurrenceViewModel? _selectedTreeNode;
    private ObjectSearchItemDto? _selectedChild;
    private CompositionItemEditor? _selectedComponent;
    private CompositionCalculationDto? _calculation;

    public CompositionWorkspaceViewModel(Action<Guid> navigate)
    {
        _navigate = navigate;
        AddChildCommand = new RelayCommand(AddChild, () => CanEdit && SelectedChild is not null);
        RemoveChildCommand = new RelayCommand(() => { if (SelectedComponent is not null) Components.Remove(SelectedComponent); },
            () => CanEdit && SelectedComponent is not null);
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<TreeOccurrenceViewModel> TreeRoots { get; } = [];
    public ObservableCollection<CompositionItemEditor> Components { get; } = [];
    public ObservableCollection<ObjectSearchItemDto> ChildResults { get; } = [];
    public ObservableCollection<CalculationDiagnosticDto> Diagnostics { get; } = [];
    public ObservableCollection<SpecificationItemDto> Specification { get; } = [];
    public RelayCommand AddChildCommand { get; }
    public RelayCommand RemoveChildCommand { get; }

    public bool CanEdit { get => _canEdit; set { if (Set(ref _canEdit, value)) RefreshCommands(); } }
    public TreeOccurrenceViewModel? SelectedTreeNode
    {
        get => _selectedTreeNode;
        set { if (Set(ref _selectedTreeNode, value) && value is not null) _navigate(value.Node.ObjectId); }
    }
    public ObjectSearchItemDto? SelectedChild
    {
        get => _selectedChild;
        set { if (Set(ref _selectedChild, value)) AddChildCommand.Refresh(); }
    }
    public CompositionItemEditor? SelectedComponent
    {
        get => _selectedComponent;
        set { if (Set(ref _selectedComponent, value)) RemoveChildCommand.Refresh(); }
    }
    public CompositionCalculationDto? Calculation
    {
        get => _calculation;
        private set { if (Set(ref _calculation, value)) { Notify(nameof(TotalMassKg)); Notify(nameof(CalculationComplete)); } }
    }
    public decimal? TotalMassKg => Calculation?.TotalMassKg;
    public bool CalculationComplete => Calculation?.IsComplete ?? false;

    public void SetTree(CompositionTreeDto tree)
    {
        TreeRoots.Clear();
        var map = new Dictionary<string, TreeOccurrenceViewModel>(StringComparer.Ordinal);
        foreach (var node in tree.Nodes.OrderBy(x => x.ObjectPath.Count))
        {
            var vm = new TreeOccurrenceViewModel(node);
            map[PathKey(node.ObjectPath)] = vm;
            if (node.ParentPath is { Count: > 0 } parentPath && map.TryGetValue(PathKey(parentPath), out var parent)) parent.Children.Add(vm);
            else TreeRoots.Add(vm);
        }
    }

    public void SetCalculation(CompositionCalculationDto? calculation)
    {
        Calculation = calculation;
        Diagnostics.Clear();
        Specification.Clear();
        if (calculation is null) return;
        foreach (var item in calculation.Diagnostics) Diagnostics.Add(item);
        foreach (var item in calculation.Items) Specification.Add(item);
    }

    public void SetItems(IEnumerable<VersionCompositionItemDto> items, Func<Guid, string> labelFor)
    {
        Components.Clear();
        foreach (var item in items) Components.Add(new CompositionItemEditor(item.ChildObjectId, item.Quantity, labelFor(item.ChildObjectId)));
    }

    public void ClearCurrent()
    {
        TreeRoots.Clear();
        SetCalculation(null);
    }

    private void AddChild()
    {
        if (SelectedChild is null) return;
        var existing = Components.FirstOrDefault(x => x.ChildObjectId == SelectedChild.Id);
        if (existing is not null)
        {
            if (!existing.TryGetPositiveQuantity(out var quantity) || quantity == int.MaxValue) return;
            existing.QuantityText = (quantity + 1).ToString(CultureInfo.InvariantCulture);
            return;
        }
        Components.Add(new CompositionItemEditor(SelectedChild.Id, 1, ObjectLabel(SelectedChild)));
    }

    private void RefreshCommands() { AddChildCommand.Refresh(); RemoveChildCommand.Refresh(); }
    private static string PathKey(IReadOnlyList<Guid> path) => string.Join("/", path);
    private static string ObjectLabel(ObjectSearchItemDto item) => item.Type == "StandardPart"
        ? item.Name ?? "Стандартное изделие" : $"{item.Designation ?? "Без обозначения"} — {item.Name ?? item.Type}";
    private bool Set<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value; Notify(name); return true;
    }
    private void Notify([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

public sealed class TreeOccurrenceViewModel
{
    public TreeOccurrenceViewModel(CompositionNodeDto node) => Node = node;
    public CompositionNodeDto Node { get; }
    public ObservableCollection<TreeOccurrenceViewModel> Children { get; } = [];
    public string Label => $"{Node.Designation ?? Node.Name ?? Node.ObjectId.ToString()}  × {Node.LocalQuantity}" +
        (Node.Error is null ? string.Empty : $"  ⚠ {Node.Error}");
}

public sealed class CompositionItemEditor(Guid childObjectId, int quantity, string label) : INotifyPropertyChanged
{
    private string _quantityText = quantity.ToString(CultureInfo.InvariantCulture);
    public Guid ChildObjectId { get; } = childObjectId;
    public string Label { get; } = label;
    public string QuantityText { get => _quantityText; set { if (_quantityText == value) return; _quantityText = value; PropertyChanged?.Invoke(this, new(nameof(QuantityText))); } }
    public bool TryGetPositiveQuantity(out int quantity) => int.TryParse(QuantityText, NumberStyles.None,
        CultureInfo.InvariantCulture, out quantity) && quantity > 0;
    public event PropertyChangedEventHandler? PropertyChanged;
}
