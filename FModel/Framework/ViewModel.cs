using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace FModel.Framework;

public class ViewModel : INotifyPropertyChanged, INotifyDataErrorInfo, IDataErrorInfo
{
    private readonly Dictionary<string, IList<string>> _validationErrors = new();

    public string this[string propertyName]
    {
        get
        {
            if (string.IsNullOrEmpty(propertyName))
                return Error;

            return _validationErrors.ContainsKey(propertyName) ? string.Join(Environment.NewLine, _validationErrors[propertyName]) : string.Empty;
        }
    }

    [JsonIgnore] public string Error => string.Join(Environment.NewLine, GetAllErrors());
    [JsonIgnore] public bool HasErrors => _validationErrors.Any();

    public IEnumerable GetErrors(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return _validationErrors.SelectMany(kvp => kvp.Value);

        return _validationErrors.TryGetValue(propertyName, out var errors) ? errors : Enumerable.Empty<object>();
    }

    private IEnumerable<string> GetAllErrors()
    {
        return _validationErrors.SelectMany(kvp => kvp.Value).Where(e => !string.IsNullOrEmpty(e));
    }

    public void AddValidationError(string propertyName, string errorMessage)
    {
        if (!_validationErrors.ContainsKey(propertyName))
            _validationErrors.Add(propertyName, new List<string>());

        _validationErrors[propertyName].Add(errorMessage);
    }

    public void ClearValidationErrors(string propertyName)
    {
        if (_validationErrors.ContainsKey(propertyName))
            _validationErrors.Remove(propertyName);
    }

    public event PropertyChangedEventHandler PropertyChanged;
#pragma warning disable 67
    public event EventHandler<DataErrorsChangedEventArgs> ErrorsChanged;
#pragma warning disable 67

    protected void RaisePropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected virtual bool SetProperty<T>(ref T storage, T value, [CallerMemberName] string propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(storage, value))
            return false;

        storage = value;
        RaisePropertyChanged(propertyName);
        return true;
    }
}