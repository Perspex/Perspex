using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.VisualTree;

namespace Avalonia.Diagnostics.ViewModels
{
    internal class ControlDetailsViewModel : ViewModelBase, IDisposable
    {
        private readonly IVisual _control;
        private readonly IDictionary<object, List<PropertyViewModel>> _propertyIndex;
        private AvaloniaPropertyViewModel? _selectedProperty;
        private bool _snapshotStyles;
        private bool _showInactiveStyles;
        private string? _styleStatus;

        public ControlDetailsViewModel(TreePageViewModel treePage, IVisual control)
        {
            _control = control;

            TreePage = treePage;

            var properties = GetAvaloniaProperties(control)
                .Concat(GetClrProperties(control))
                .OrderBy(x => x, PropertyComparer.Instance)
                .ThenBy(x => x.Name)
                .ToList();

            _propertyIndex = properties.GroupBy(x => x.Key).ToDictionary(x => x.Key, x => x.ToList());

            var view = new DataGridCollectionView(properties);
            view.GroupDescriptions.Add(new DataGridPathGroupDescription(nameof(AvaloniaPropertyViewModel.Group)));
            view.Filter = FilterProperty;
            PropertiesView = view;

            Layout = new ControlLayoutViewModel(control);

            if (control is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged += ControlPropertyChanged;
            }

            if (control is AvaloniaObject ao)
            {
                ao.PropertyChanged += ControlPropertyChanged;
            }

            AppliedStyles = new ObservableCollection<StyleViewModel>();
            PseudoClasses = new ObservableCollection<PseudoClassViewModel>();

            if (control is StyledElement styledElement)
            {
                styledElement.Classes.CollectionChanged += OnClassesChanged;

                var pseudoClassAttributes = styledElement.GetType().GetCustomAttributes<PseudoClassesAttribute>(true);

                foreach (var classAttribute in pseudoClassAttributes)
                {
                    foreach (var className in classAttribute.PseudoClasses)
                    {
                        PseudoClasses.Add(new PseudoClassViewModel(className, styledElement));
                    }
                }

                var styleDiagnostics = styledElement.GetStyleDiagnostics();

                foreach (var appliedStyle in styleDiagnostics.AppliedStyles)
                {
                    var styleSource = appliedStyle.Source;

                    var setters = new List<SetterViewModel>();

                    if (styleSource is Style style)
                    {
                        foreach (var setter in style.Setters)
                        {
                            if (setter is Setter regularSetter
                                && regularSetter.Property != null)
                            {
                                var setterValue = regularSetter.Value;

                                var resourceInfo = GetResourceInfo(setterValue);

                                SetterViewModel setterVm;

                                if (resourceInfo.HasValue)
                                {
                                    var resourceKey = resourceInfo.Value.resourceKey;
                                    var resourceValue = styledElement.FindResource(resourceKey);

                                    setterVm = new ResourceSetterViewModel(regularSetter.Property, resourceKey, resourceValue, resourceInfo.Value.isDynamic);
                                }
                                else
                                {
                                    setterVm = new SetterViewModel(regularSetter.Property, setterValue);
                                }

                                setters.Add(setterVm);
                            }
                        }

                        AppliedStyles.Add(new StyleViewModel(appliedStyle, style.Selector?.ToString() ?? "No selector", setters));
                    }
                }

                UpdateStyles();
            }
        }

        private (object resourceKey, bool isDynamic)? GetResourceInfo(object? value)
        {
            if (value is StaticResourceExtension staticResource)
            {
                return (staticResource.ResourceKey, false);
            }
            else if (value is DynamicResourceExtension dynamicResource
                && dynamicResource.ResourceKey != null)
            {
                return (dynamicResource.ResourceKey, true);
            }

            return null;
        }

        public TreePageViewModel TreePage { get; }

        public DataGridCollectionView PropertiesView { get; }

        public ObservableCollection<StyleViewModel> AppliedStyles { get; }

        public ObservableCollection<PseudoClassViewModel> PseudoClasses { get; }

        public AvaloniaPropertyViewModel? SelectedProperty
        {
            get => _selectedProperty;
            set => RaiseAndSetIfChanged(ref _selectedProperty, value);
        }

        public bool SnapshotStyles
        {
            get => _snapshotStyles;
            set => RaiseAndSetIfChanged(ref _snapshotStyles, value);
        }

        public bool ShowInactiveStyles
        {
            get => _showInactiveStyles;
            set => RaiseAndSetIfChanged(ref _showInactiveStyles, value);
        }

        public string? StyleStatus
        {
            get => _styleStatus;
            set => RaiseAndSetIfChanged(ref _styleStatus, value);
        }

        public ControlLayoutViewModel Layout { get; }

        protected override void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);

            if (e.PropertyName == nameof(SnapshotStyles))
            {
                if (!SnapshotStyles)
                {
                    UpdateStyles();
                }
            }
        }

        public void UpdateStyleFilters()
        {
            foreach (var style in AppliedStyles)
            {
                var hasVisibleSetter = false;

                foreach (var setter in style.Setters)
                {
                    setter.IsVisible = TreePage.SettersFilter.Filter(setter.Name);

                    hasVisibleSetter |= setter.IsVisible;
                }

                style.IsVisible = hasVisibleSetter;
            }
        }

        public void Dispose()
        {
            if (_control is INotifyPropertyChanged inpc)
            {
                inpc.PropertyChanged -= ControlPropertyChanged;
            }

            if (_control is AvaloniaObject ao)
            {
                ao.PropertyChanged -= ControlPropertyChanged;
            }

            if (_control is StyledElement se)
            {
                se.Classes.CollectionChanged -= OnClassesChanged;
            }
        }

        private IEnumerable<PropertyViewModel> GetAvaloniaProperties(object o)
        {
            if (o is AvaloniaObject ao)
            {
                return AvaloniaPropertyRegistry.Instance.GetRegistered(ao)
                    .Union(AvaloniaPropertyRegistry.Instance.GetRegisteredAttached(ao.GetType()))
                    .Select(x => new AvaloniaPropertyViewModel(ao, x));
            }
            else
            {
                return Enumerable.Empty<AvaloniaPropertyViewModel>();
            }
        }

        private IEnumerable<PropertyViewModel> GetClrProperties(object o)
        {
            foreach (var p in GetClrProperties(o, o.GetType()))
            {
                yield return p;
            }

            foreach (var i in o.GetType().GetInterfaces())
            {
                foreach (var p in GetClrProperties(o, i))
                {
                    yield return p;
                }
            }
        }

        private IEnumerable<PropertyViewModel> GetClrProperties(object o, Type t)
        {
            return t.GetProperties()
                .Where(x => x.GetIndexParameters().Length == 0)
                .Select(x => new ClrPropertyViewModel(o, x));
        }

        private void ControlPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            if (_propertyIndex.TryGetValue(e.Property, out var properties))
            {
                foreach (var property in properties)
                {
                    property.Update();
                }
            }

            Layout.ControlPropertyChanged(sender, e);
        }

        private void ControlPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != null
                && _propertyIndex.TryGetValue(e.PropertyName, out var properties))
            {
                foreach (var property in properties)
                {
                    property.Update();
                }
            }

            if (!SnapshotStyles)
            {
                UpdateStyles();
            }
        }

        private void OnClassesChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (!SnapshotStyles)
            {
                UpdateStyles();
            }
        }

        private void UpdateStyles()
        {
            int activeCount = 0;

            foreach (var style in AppliedStyles)
            {
                style.Update();

                if (style.IsActive)
                {
                    activeCount++;
                }
            }

            var propertyBuckets = new Dictionary<AvaloniaProperty, List<SetterViewModel>>();

            foreach (var style in AppliedStyles)
            {
                if (!style.IsActive)
                {
                    continue;
                }

                foreach (var setter in style.Setters)
                {
                    if (propertyBuckets.TryGetValue(setter.Property, out var setters))
                    {
                        foreach (var otherSetter in setters)
                        {
                            otherSetter.IsActive = false;
                        }

                        setter.IsActive = true;

                        setters.Add(setter);
                    }
                    else
                    {
                        setter.IsActive = true;

                        setters = new List<SetterViewModel> { setter };

                        propertyBuckets.Add(setter.Property, setters);
                    }
                }
            }

            foreach (var pseudoClass in PseudoClasses)
            {
                pseudoClass.Update();
            }

            StyleStatus = $"Styles ({activeCount}/{AppliedStyles.Count} active)";
        }

        private bool FilterProperty(object arg)
        {
            return !(arg is PropertyViewModel property) || TreePage.PropertiesFilter.Filter(property.Name);
        }

        private class PropertyComparer : IComparer<PropertyViewModel>
        {
            public static PropertyComparer Instance { get; } = new PropertyComparer();

            public int Compare(PropertyViewModel? x, PropertyViewModel? y)
            {
                var groupX = GroupIndex(x?.Group);
                var groupY = GroupIndex(y?.Group);

                if (groupX != groupY)
                {
                    return groupX - groupY;
                }
                else
                {
                    return string.CompareOrdinal(x?.Name, y?.Name);
                }
            }

            private int GroupIndex(string? group)
            {
                switch (group)
                {
                    case "Properties": return 0;
                    case "Attached Properties": return 1;
                    case "CLR Properties": return 2;
                    default: return 3;
                }
            }
        }
    }
}
