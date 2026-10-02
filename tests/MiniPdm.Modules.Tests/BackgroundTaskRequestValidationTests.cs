using System.ComponentModel.DataAnnotations;
using MiniPdm.Contracts.Modules.BackgroundTasks.DtoModels;
using Xunit;

namespace MiniPdm.Modules.Tests;

/// <summary>
/// Проверяет ограничения интервала в публичном запросе изменения расписания.
/// </summary>
public sealed class BackgroundTaskRequestValidationTests
{
    /// <summary>
    /// Проверяет наличие ограничения допустимого диапазона интервалов у параметра DTO расписания.
    /// </summary>
    [Fact]
    public void Schedule_interval_range_is_attached_to_record_constructor_parameter()
    {
        var parameter = Assert.Single(typeof(UpdateBackgroundTaskScheduleRequestDto).GetConstructors().Single().GetParameters());
        var range = parameter.GetCustomAttributes(typeof(RangeAttribute), inherit: true).Cast<RangeAttribute>().Single();

        Assert.False(range.IsValid(0));
        Assert.True(range.IsValid(1));
        Assert.True(range.IsValid(525600));
        Assert.False(range.IsValid(525601));
    }
}
