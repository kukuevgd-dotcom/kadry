using System.Collections.ObjectModel;
using Kadry.App.Infrastructure;
using Kadry.App.Services;
using Kadry.Core.Domain;
using Kadry.Core.Services;

namespace Kadry.App.ViewModels;

/// <summary>Экран «Справочники»: отделы, должности, виды отпусков.</summary>
public class DirectoriesViewModel : ViewModelBase
{
    private readonly DirectoryService _service;
    private readonly IUiDialogs _dialogs;

    public DirectoriesViewModel(DirectoryService service, IUiDialogs dialogs)
    {
        _service = service;
        _dialogs = dialogs;

        AddDepartmentCommand = new RelayCommand(_ => AddDepartment());
        RenameDepartmentCommand = new RelayCommand(_ => RenameDepartment(), _ => SelectedDepartment is not null);
        DeleteDepartmentCommand = new RelayCommand(_ => DeleteDepartment(), _ => SelectedDepartment is not null);

        AddPositionCommand = new RelayCommand(_ => AddPosition());
        RenamePositionCommand = new RelayCommand(_ => RenamePosition(), _ => SelectedPosition is not null);
        DeletePositionCommand = new RelayCommand(_ => DeletePosition(), _ => SelectedPosition is not null);

        AddVacationTypeCommand = new RelayCommand(_ => AddVacationType());
        EditVacationTypeCommand = new RelayCommand(_ => EditVacationType(), _ => SelectedVacationType is not null);
        DeleteVacationTypeCommand = new RelayCommand(_ => DeleteVacationType(), _ => SelectedVacationType is not null);

        Reload();
    }

    public ObservableCollection<Department> Departments { get; } = new();

    public ObservableCollection<Position> Positions { get; } = new();

    public ObservableCollection<VacationType> VacationTypes { get; } = new();

    private Department? _selectedDepartment;
    private Position? _selectedPosition;
    private VacationType? _selectedVacationType;

    public Department? SelectedDepartment
    {
        get => _selectedDepartment;
        set => SetProperty(ref _selectedDepartment, value);
    }

    public Position? SelectedPosition
    {
        get => _selectedPosition;
        set => SetProperty(ref _selectedPosition, value);
    }

    public VacationType? SelectedVacationType
    {
        get => _selectedVacationType;
        set => SetProperty(ref _selectedVacationType, value);
    }

    public RelayCommand AddDepartmentCommand { get; }
    public RelayCommand RenameDepartmentCommand { get; }
    public RelayCommand DeleteDepartmentCommand { get; }

    public RelayCommand AddPositionCommand { get; }
    public RelayCommand RenamePositionCommand { get; }
    public RelayCommand DeletePositionCommand { get; }

    public RelayCommand AddVacationTypeCommand { get; }
    public RelayCommand EditVacationTypeCommand { get; }
    public RelayCommand DeleteVacationTypeCommand { get; }

    private void Reload()
    {
        Departments.Clear();
        foreach (var department in _service.Departments())
            Departments.Add(department);

        Positions.Clear();
        foreach (var position in _service.Positions())
            Positions.Add(position);

        VacationTypes.Clear();
        foreach (var type in _service.VacationTypes())
            VacationTypes.Add(type);
    }

    private void AddDepartment()
    {
        string? name = _dialogs.EditText("Новый отдел", "Название отдела", string.Empty);
        if (name is null)
            return;

        TryRun(() => _service.AddDepartment(name));
    }

    private void RenameDepartment()
    {
        if (SelectedDepartment is null)
            return;

        string? name = _dialogs.EditText("Переименование отдела", "Название отдела", SelectedDepartment.Name);
        if (name is null)
            return;

        TryRun(() => _service.RenameDepartment(SelectedDepartment.Id, name));
    }

    private void DeleteDepartment()
    {
        if (SelectedDepartment is null)
            return;

        if (!_dialogs.Confirm("Удаление отдела", $"Удалить отдел «{SelectedDepartment.Name}»?"))
            return;

        TryRun(() => _service.DeleteDepartment(SelectedDepartment.Id));
    }

    private void AddPosition()
    {
        string? name = _dialogs.EditText("Новая должность", "Название должности", string.Empty);
        if (name is null)
            return;

        TryRun(() => _service.AddPosition(name));
    }

    private void RenamePosition()
    {
        if (SelectedPosition is null)
            return;

        string? name = _dialogs.EditText("Переименование должности", "Название должности", SelectedPosition.Name);
        if (name is null)
            return;

        TryRun(() => _service.RenamePosition(SelectedPosition.Id, name));
    }

    private void DeletePosition()
    {
        if (SelectedPosition is null)
            return;

        if (!_dialogs.Confirm("Удаление должности", $"Удалить должность «{SelectedPosition.Name}»?"))
            return;

        TryRun(() => _service.DeletePosition(SelectedPosition.Id));
    }

    private void AddVacationType()
    {
        var input = _dialogs.EditVacationType();
        if (input is null)
            return;

        TryRun(() => _service.AddVacationType(input.Name, input.YearLimitDays, input.IsUnpaid));
    }

    private void EditVacationType()
    {
        if (SelectedVacationType is null)
            return;

        var input = _dialogs.EditVacationType(new VacationTypeInput(
            SelectedVacationType.Name,
            SelectedVacationType.YearLimitDays,
            SelectedVacationType.IsUnpaid));
        if (input is null)
            return;

        TryRun(() => _service.UpdateVacationType(
            SelectedVacationType.Id, input.Name, input.YearLimitDays, input.IsUnpaid));
    }

    private void DeleteVacationType()
    {
        if (SelectedVacationType is null)
            return;

        if (!_dialogs.Confirm("Удаление вида отпуска", $"Удалить «{SelectedVacationType.Name}»?"))
            return;

        TryRun(() => _service.DeleteVacationType(SelectedVacationType.Id));
    }

    private void TryRun(Action action)
    {
        try
        {
            action();
            Reload();
        }
        catch (Exception ex)
        {
            _dialogs.Error(ex.Message);
        }
    }
}
