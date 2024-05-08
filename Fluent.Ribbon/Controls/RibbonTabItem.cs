// ReSharper disable once CheckNamespace
namespace Fluent;

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using Fluent.Automation.Peers;
using Fluent.Extensions;
using Fluent.Helpers;
using Fluent.Internal;
using Fluent.Internal.KnownBoxes;

/// <summary>
/// Represents ribbon tab item
/// </summary>
[TemplatePart(Name = "PART_HeaderContentHost", Type = typeof(FrameworkElement))]
[TemplatePart(Name = "PART_ContentContainer", Type = typeof(Border))]
[ContentProperty(nameof(Groups))]
[DefaultProperty(nameof(Groups))]
[DebuggerDisplay("{GetType().FullName}: Header = {Header}, Groups.Count = {Groups.Count}, IsSimplified = {IsSimplified}")]
public class RibbonTabItem : Control, IKeyTipedControl, IHeaderedControl, ILogicalChildSupport, ISimplifiedStateControl
{
    #region Fields

    // Content container
    private Border? contentContainer;

    // Collection of ribbon groups
    private ObservableCollection<RibbonGroupBox>? groups;

    // Ribbon groups container
    private readonly RibbonGroupsContainer groupsInnerContainer = new();

    // Cached width
    private double cachedWidth;

    #endregion

    #region Properties

    internal FrameworkElement? HeaderContentHost { get; private set; }

    #region Colors/Brushes

    /// <summary>
    /// Gets or sets the <see cref="Brush"/> which is used to render the background if this <see cref="RibbonTabItem"/> is the currently active/selected one.
    /// </summary>
    public Brush? ActiveTabBackground
    {
        get => (Brush?)this.GetValue(ActiveTabBackgroundProperty);
        set => this.SetValue(ActiveTabBackgroundProperty, value);
    }

    /// <summary>Identifies the <see cref="ActiveTabBackground"/> dependency property.</summary>
    public static readonly DependencyProperty ActiveTabBackgroundProperty =
        DependencyProperty.Register(nameof(ActiveTabBackground), typeof(Brush), typeof(RibbonTabItem), new PropertyMetadata());

    /// <summary>
    /// Gets or sets the <see cref="Brush"/> which is used to render the border if this <see cref="RibbonTabItem"/> is the currently active/selected one.
    /// </summary>
    public Brush? ActiveTabBorderBrush
    {
        get => (Brush?)this.GetValue(ActiveTabBorderBrushProperty);
        set => this.SetValue(ActiveTabBorderBrushProperty, value);
    }

    /// <summary>Identifies the <see cref="ActiveTabBorderBrush"/> dependency property.</summary>
    public static readonly DependencyProperty ActiveTabBorderBrushProperty =
        DependencyProperty.Register(nameof(ActiveTabBorderBrush), typeof(Brush), typeof(RibbonTabItem), new PropertyMetadata());

    #endregion

    #region KeyTip

    /// <inheritdoc />
    public string? KeyTip
    {
        get => (string?)this.GetValue(KeyTipProperty);
        set => this.SetValue(KeyTipProperty, value);
    }

    /// <summary>
    /// Using a DependencyProperty as the backing store for Keys.
    /// This enables animation, styling, binding, etc...
    /// </summary>
    public static readonly DependencyProperty KeyTipProperty = Fluent.KeyTip.KeysProperty.AddOwner(typeof(RibbonTabItem));

    #endregion

    /// <summary>
    /// Gets ribbon groups container
    /// </summary>
    public ScrollViewer GroupsContainer { get; }

    /// <summary>
    /// Gets or sets reduce order
    /// </summary>
    public string? ReduceOrder
    {
        get => this.groupsInnerContainer.ReduceOrder;
        set => this.groupsInnerContainer.ReduceOrder = value;
    }

    #region IsContextual

    /// <summary>
    /// Gets or sets whether tab item is contextual
    /// </summary>
    public bool IsContextual
    {
        get => (bool)this.GetValue(IsContextualProperty);
        private set => this.SetValue(IsContextualPropertyKey, BooleanBoxes.Box(value));
    }

    private static readonly DependencyPropertyKey IsContextualPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IsContextual), typeof(bool), typeof(RibbonTabItem), new PropertyMetadata(BooleanBoxes.FalseBox));

    /// <summary>Identifies the <see cref="IsContextual"/> dependency property.</summary>
    public static readonly DependencyProperty IsContextualProperty = IsContextualPropertyKey.DependencyProperty;

    #endregion

    /// <summary>
    /// Gets or sets whether tab item is selected
    /// </summary>
    [Bindable(true)]
    [Category("Appearance")]
    public bool IsSelected
    {
        get => (bool)this.GetValue(IsSelectedProperty);

        set => this.SetValue(IsSelectedProperty, BooleanBoxes.Box(value));
    }

    /// <summary>
    /// Using a DependencyProperty as the backing store for IsSelected.
    /// This enables animation, styling, binding, etc...
    /// </summary>
    public static readonly DependencyProperty IsSelectedProperty = Selector.IsSelectedProperty.AddOwner(typeof(RibbonTabItem), new FrameworkPropertyMetadata(BooleanBoxes.FalseBox, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnIsSelectedChanged));

    /// <summary>
    /// Gets ribbon tab control parent
    /// </summary>
    internal RibbonTabControl? TabControlParent => UIHelper.GetParent<RibbonTabControl>(this);

    /// <summary>
    /// Gets or sets the padding for the header.
    /// </summary>
    public Thickness HeaderPadding
    {
        get => (Thickness)this.GetValue(HeaderPaddingProperty);
        set => this.SetValue(HeaderPaddingProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderPadding"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderPaddingProperty = DependencyProperty.Register(nameof(HeaderPadding), typeof(Thickness), typeof(RibbonTabItem), new FrameworkPropertyMetadata(new Thickness(9, 3, 9, 6), FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

    /// <summary>Identifies the <see cref="SeparatorOpacity"/> dependency property.</summary>
    public static readonly DependencyProperty SeparatorOpacityProperty = DependencyProperty.Register(nameof(SeparatorOpacity), typeof(double), typeof(RibbonTabItem), new PropertyMetadata(DoubleBoxes.Zero));

    /// <summary>
    /// Gets or sets the opacity of the separator.
    /// </summary>
    public double SeparatorOpacity
    {
        get => (double)this.GetValue(SeparatorOpacityProperty);
        set => this.SetValue(SeparatorOpacityProperty, value);
    }

    /// <summary>
    /// Gets or sets ribbon contextual tab group
    /// </summary>
    public RibbonContextualTabGroup? Group
    {
        get => (RibbonContextualTabGroup?)this.GetValue(GroupProperty);
        set => this.SetValue(GroupProperty, value);
    }

    /// <summary>Identifies the <see cref="Group"/> dependency property.</summary>
    public static readonly DependencyProperty GroupProperty =
        DependencyProperty.Register(nameof(Group), typeof(RibbonContextualTabGroup), typeof(RibbonTabItem), new PropertyMetadata(OnGroupChanged));

    // handles Group property chanhged
    private static void OnGroupChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var tab = (RibbonTabItem)d;

        ((RibbonContextualTabGroup?)e.OldValue)?.RemoveTabItem(tab);

        if (e.NewValue is RibbonContextualTabGroup tabGroup)
        {
            tabGroup.AppendTabItem(tab);
            tab.IsContextual = true;
        }
        else
        {
            tab.IsContextual = false;
        }
    }

    /// <summary>
    /// Gets or sets whether tab item has left group border
    /// </summary>
    public bool HasLeftGroupBorder
    {
        get => (bool)this.GetValue(HasLeftGroupBorderProperty);
        set => this.SetValue(HasLeftGroupBorderProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="HasLeftGroupBorder"/> dependency property.</summary>
    public static readonly DependencyProperty HasLeftGroupBorderProperty =
        DependencyProperty.Register(nameof(HasLeftGroupBorder), typeof(bool), typeof(RibbonTabItem), new PropertyMetadata(BooleanBoxes.FalseBox));

    /// <summary>
    /// Gets or sets whether tab item has right group border
    /// </summary>
    public bool HasRightGroupBorder
    {
        get => (bool)this.GetValue(HasRightGroupBorderProperty);
        set => this.SetValue(HasRightGroupBorderProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="HasRightGroupBorder"/> dependency property.</summary>
    public static readonly DependencyProperty HasRightGroupBorderProperty =
        DependencyProperty.Register(nameof(HasRightGroupBorder), typeof(bool), typeof(RibbonTabItem), new PropertyMetadata(BooleanBoxes.FalseBox));

    /// <summary>
    /// get collection of ribbon groups
    /// </summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Content)]
    public ObservableCollection<RibbonGroupBox> Groups
    {
        get
        {
            if (this.groups is null)
            {
                this.groups = new ObservableCollection<RibbonGroupBox>();
                this.groups.CollectionChanged += this.OnGroupsCollectionChanged;
            }

            return this.groups;
        }
    }

    // handles ribbon groups collection changes
    private void OnGroupsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        switch (e.Action)
        {
            case NotifyCollectionChangedAction.Add:
            {
                var isSimplified = this.IsSimplified;
                for (var i = 0; i < e.NewItems?.Count; i++)
                {
                    var element = (UIElement?)e.NewItems![i];

                    if (element is not null)
                    {
                        this.groupsInnerContainer.Children.Insert(e.NewStartingIndex + i, element);
                    }

                    if (element is ISimplifiedStateControl control)
                    {
                        control.UpdateSimplifiedState(isSimplified);
                    }
                }
            }

                break;

            case NotifyCollectionChangedAction.Remove:
                foreach (var item in e.OldItems.NullSafe().OfType<UIElement>())
                {
                    this.groupsInnerContainer.Children.Remove(item);
                }

                break;

            case NotifyCollectionChangedAction.Replace:
                foreach (var item in e.OldItems.NullSafe().OfType<UIElement>())
                {
                    this.groupsInnerContainer.Children.Remove(item);
                }

            {
                var isSimplified = this.IsSimplified;
                foreach (var item in e.NewItems.NullSafe().OfType<UIElement>())
                {
                    this.groupsInnerContainer.Children.Add(item);

                    if (item is ISimplifiedStateControl control)
                    {
                        control.UpdateSimplifiedState(isSimplified);
                    }
                }
            }

                break;

            case NotifyCollectionChangedAction.Reset:
                this.groupsInnerContainer.Children.Clear();

            {
                var isSimplified = this.IsSimplified;
                foreach (var group in this.Groups)
                {
                    this.groupsInnerContainer.Children.Add(group);

                    if (group is ISimplifiedStateControl control)
                    {
                        control.UpdateSimplifiedState(isSimplified);
                    }
                }
            }

                break;
        }
    }

    #region Header Property

    /// <inheritdoc />
    public object? Header
    {
        get => this.GetValue(HeaderProperty);
        set => this.SetValue(HeaderProperty, value);
    }

    /// <summary>Identifies the <see cref="Header"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderProperty = RibbonControl.HeaderProperty.AddOwner(typeof(RibbonTabItem), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsMeasure, LogicalChildSupportHelper.OnLogicalChildPropertyChanged));
        
    /// <inheritdoc />
    public DataTemplate? HeaderTemplate
    {
        get => (DataTemplate?)this.GetValue(HeaderTemplateProperty);
        set => this.SetValue(HeaderTemplateProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateProperty = RibbonControl.HeaderTemplateProperty.AddOwner(typeof(RibbonTabItem), new PropertyMetadata());

    /// <inheritdoc />
    public DataTemplateSelector? HeaderTemplateSelector
    {
        get => (DataTemplateSelector?)this.GetValue(HeaderTemplateSelectorProperty);
        set => this.SetValue(HeaderTemplateSelectorProperty, value);
    }

    /// <summary>Identifies the <see cref="HeaderTemplateSelector"/> dependency property.</summary>
    public static readonly DependencyProperty HeaderTemplateSelectorProperty = RibbonControl.HeaderTemplateSelectorProperty.AddOwner(typeof(RibbonTabItem), new PropertyMetadata());

    #endregion

    #region Focusable

    /// <summary>
    /// Handles Focusable changes
    /// </summary>
    private static void OnFocusableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
    }

    /// <summary>
    /// Coerces Focusable
    /// </summary>
    private static object? CoerceFocusable(DependencyObject d, object? basevalue)
    {
        var control = d as RibbonTabItem;
        var ribbon = control?.FindParentRibbon();

        if (ribbon is not null
            && basevalue is bool boolValue)
        {
            return BooleanBoxes.Box(boolValue && ribbon.Focusable);
        }

        return basevalue;
    }

    // Find parent ribbon
    private Ribbon? FindParentRibbon()
    {
        var element = this.Parent;
        while (element is not null)
        {
            if (element is Ribbon ribbon)
            {
                return ribbon;
            }

            element = VisualTreeHelper.GetParent(element);
        }

        return null;
    }

    #endregion

    #region IsSimplified

    /// <summary>
    /// Gets or sets whether or not the ribbon is in Simplified mode
    /// </summary>
    public bool IsSimplified
    {
        get => (bool)this.GetValue(IsSimplifiedProperty);
        private set => this.SetValue(IsSimplifiedPropertyKey, BooleanBoxes.Box(value));
    }

    private static readonly DependencyPropertyKey IsSimplifiedPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IsSimplified), typeof(bool), typeof(RibbonTabItem), new PropertyMetadata(BooleanBoxes.FalseBox, OnIsSimplifiedChanged));

    /// <summary>Identifies the <see cref="IsSimplified"/> dependency property.</summary>
    public static readonly DependencyProperty IsSimplifiedProperty = IsSimplifiedPropertyKey.DependencyProperty;

    private static void OnIsSimplifiedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RibbonTabItem ribbonTabItem)
        {
            var isSimplified = (bool)e.NewValue;
            foreach (var item in ribbonTabItem.Groups.OfType<ISimplifiedStateControl>())
            {
                item.UpdateSimplifiedState(isSimplified);
            }
        }
    }
    #endregion

    #endregion Properties

    #region Initialize

    /// <summary>
    /// Static constructor
    /// </summary>
    static RibbonTabItem()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(RibbonTabItem), new FrameworkPropertyMetadata(typeof(RibbonTabItem)));
        FocusableProperty.AddOwner(typeof(RibbonTabItem), new FrameworkPropertyMetadata(OnFocusableChanged, CoerceFocusable));
        VisibilityProperty.AddOwner(typeof(RibbonTabItem), new FrameworkPropertyMetadata(OnVisibilityChanged));

        KeyboardNavigation.DirectionalNavigationProperty.OverrideMetadata(typeof(RibbonTabItem), new FrameworkPropertyMetadata(KeyboardNavigationMode.Contained));
        KeyboardNavigation.TabNavigationProperty.OverrideMetadata(typeof(RibbonTabItem), new FrameworkPropertyMetadata(KeyboardNavigationMode.Local));

        AutomationProperties.IsOffscreenBehaviorProperty.OverrideMetadata(typeof(RibbonTabItem), new FrameworkPropertyMetadata(IsOffscreenBehavior.FromClip));
    }

    // Handles visibility changes
    private static void OnVisibilityChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var item = d as RibbonTabItem;

        if (item is null)
        {
            return;
        }

        item.Group?.UpdateInnerVisiblityAndGroupBorders();

        if (item.IsSelected
            && (Visibility)e.NewValue == Visibility.Collapsed)
        {
            if (item.TabControlParent is not null)
            {
                if (item.TabControlParent.IsMinimized)
                {
                    item.IsSelected = false;
                }
                else
                {
                    item.TabControlParent.SelectedItem = item.TabControlParent.GetFirstVisibleAndEnabledItem();
                }
            }
        }
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public RibbonTabItem()
    {
        this.GroupsContainer = new RibbonGroupsContainerScrollViewer();
        this.AddLogicalChild(this.GroupsContainer);
        this.GroupsContainer.Content = this.groupsInnerContainer;

        // Force redirection of DataContext. This is needed, because we detach the container from the visual tree and attach it to a diffrent one (the popup/dropdown) when the ribbon is minimized.
        this.groupsInnerContainer.SetBinding(DataContextProperty, new Binding(nameof(this.DataContext))
        {
            Source = this
        });

        this.groupsInnerContainer.SetBinding(MarginProperty, new Binding(nameof(this.Padding))
        {
            Source = this
        });

        ContextMenuService.Coerce(this);

        this.Loaded += this.OnLoaded;
        this.Unloaded += this.OnUnloaded;
    }

    #endregion

    #region Overrides

    /// <inheritdoc />
    protected override Size MeasureOverride(Size constraint)
    {
        if (this.contentContainer is null)
        {
            return base.MeasureOverride(constraint);
        }

        if (this.IsContextual
            && this.Group?.Visibility == Visibility.Collapsed)
        {
            return Size.Empty;
        }

        var baseConstraint = base.MeasureOverride(constraint);

        if (DoubleUtil.AreClose(this.cachedWidth, baseConstraint.Width) == false
            && this.IsContextual
            && this.Group is not null)
        {
            this.cachedWidth = baseConstraint.Width;

            var contextualTabGroupContainer = UIHelper.GetParent<RibbonContextualGroupsContainer>(this.Group);
            contextualTabGroupContainer?.InvalidateMeasure();

            var ribbonTitleBar = UIHelper.GetParent<RibbonTitleBar>(this.Group);
            ribbonTitleBar?.ScheduleForceMeasureAndArrange();
        }

        return baseConstraint;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size arrangeBounds)
    {
        var result = base.ArrangeOverride(arrangeBounds);

        var ribbonTitleBar = UIHelper.GetParent<RibbonTitleBar>(this.Group);
        ribbonTitleBar?.ScheduleForceMeasureAndArrange();

        return result;
    }

    /// <inheritdoc />
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();

        this.HeaderContentHost = this.GetTemplateChild("PART_HeaderContentHost") as FrameworkElement;

        this.contentContainer = this.GetTemplateChild("PART_ContentContainer") as Border;
    }

    /// <inheritdoc />
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        if (ReferenceEquals(e.Source, this)
            && e.ClickCount == 2)
        {
            e.Handled = true;

            if (this.TabControlParent is not null)
            {
                var canMinimize = this.TabControlParent.CanMinimize;
                if (canMinimize)
                {
                    this.TabControlParent.IsMinimized = !this.TabControlParent.IsMinimized;
                }
            }
        }
        else if (ReferenceEquals(e.Source, this)
                 || this.IsSelected == false)
        {
            if (this.Visibility == Visibility.Visible)
            {
                if (this.TabControlParent is not null)
                {
                    var newItem = this.TabControlParent.ItemContainerGenerator.ItemFromContainerOrContainerContent(this);

                    if (ReferenceEquals(this.TabControlParent.SelectedItem, newItem))
                    {
                        this.TabControlParent.IsDropDownOpen = !this.TabControlParent.IsDropDownOpen;
                    }
                    else
                    {
                        this.TabControlParent.SelectedItem = newItem;
                        this.TabControlParent.IsDropDownOpen = true;
                    }

                    this.TabControlParent.RaiseRequestBackstageClose();
                }
                else
                {
                    this.IsSelected = true;
                }

                e.Handled = true;
            }
        }
    }

    /// <inheritdoc />
    protected override void OnKeyDown(KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Enter:
            case Key.Space:
                if (this.TabControlParent is not null
                    && this.TabControlParent.IsMinimized)
                {
                    this.TabControlParent.IsDropDownOpen = true;

                    e.Handled = true;
                }

                break;
        }

        base.OnKeyDown(e);
    }

    /// <inheritdoc />
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e)
    {
        base.OnGotKeyboardFocus(e);

        this.SetCurrentValue(IsSelectedProperty, BooleanBoxes.TrueBox);
    }

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new RibbonTabItemAutomationPeer(this);

    #endregion

    #region Private methods

    // Handles IsSelected property changes
    private static void OnIsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var container = (RibbonTabItem)d;
        var newValue = (bool)e.NewValue;

        if (newValue)
        {
            container.OnSelected(new RoutedEventArgs(Selector.SelectedEvent, container));
            container.BringIntoView();
        }
        else
        {
            container.OnUnselected(new RoutedEventArgs(Selector.UnselectedEvent, container));
        }

        // Raise UI automation events on this RibbonTabItem
        if (AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementSelected)
            || AutomationPeer.ListenerExists(AutomationEvents.SelectionItemPatternOnElementRemovedFromSelection))
        {
            //SelectorHelper.RaiseIsSelectedChangedAutomationEvent(container.TabControlParent, container, newValue);
            var peer = UIElementAutomationPeer.CreatePeerForElement(container) as RibbonTabItemAutomationPeer;
            peer?.RaiseTabSelectionEvents();
        }
    }

    /// <summary>
    /// Handles selected
    /// </summary>
    /// <param name="e">The event data</param>
    protected virtual void OnSelected(RoutedEventArgs e)
    {
        this.HandleIsSelectedChanged(e);
    }

    /// <summary>
    /// handles unselected
    /// </summary>
    /// <param name="e">The event data</param>
    protected virtual void OnUnselected(RoutedEventArgs e)
    {
        this.HandleIsSelectedChanged(e);
    }

    #endregion

    #region Event handling

    // Handles IsSelected property changes
    private void HandleIsSelectedChanged(RoutedEventArgs e)
    {
        this.RaiseEvent(e);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        this.SubscribeEvents();
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.UnSubscribeEvents();
    }

    private void SubscribeEvents()
    {
        // Always unsubscribe events to ensure we don't subscribe twice
        this.UnSubscribeEvents();

        if (this.groups is not null)
        {
            this.groups.CollectionChanged += this.OnGroupsCollectionChanged;
        }
    }

    private void UnSubscribeEvents()
    {
        if (this.groups is not null)
        {
            this.groups.CollectionChanged -= this.OnGroupsCollectionChanged;
        }
    }

    #endregion

    /// <inheritdoc />
    public KeyTipPressedResult OnKeyTipPressed()
    {
        this.SetCurrentValue(IsSelectedProperty, BooleanBoxes.TrueBox);

        var result = KeyTipPressedResult.Empty;

        if (this.TabControlParent is not null
            && this.TabControlParent.IsMinimized)
        {
            this.TabControlParent.IsDropDownOpen = true;

            result = new KeyTipPressedResult(true, true);
        }

        // This way keytips for delay loaded elements work correctly. Partially fixes #244.
        this.Dispatcher.Invoke(System.Windows.Threading.DispatcherPriority.Background, new Action(() => { }));

        return result;
    }

    /// <inheritdoc />
    public void OnKeyTipBack()
    {
        if (this.TabControlParent is not null
            && this.TabControlParent.IsMinimized)
        {
            this.TabControlParent.IsDropDownOpen = false;
        }
    }

    /// <inheritdoc />
    void ISimplifiedStateControl.UpdateSimplifiedState(bool isSimplified)
    {
        this.IsSimplified = isSimplified;
    }

    /// <inheritdoc />
    void ILogicalChildSupport.AddLogicalChild(object child)
    {
        this.AddLogicalChild(child);
    }

    /// <inheritdoc />
    void ILogicalChildSupport.RemoveLogicalChild(object child)
    {
        this.RemoveLogicalChild(child);
    }

    /// <inheritdoc />
    protected override IEnumerator LogicalChildren
    {
        get
        {
            var baseEnumerator = base.LogicalChildren;
            while (baseEnumerator?.MoveNext() == true)
            {
                yield return baseEnumerator.Current;
            }

            yield return this.GroupsContainer;

            if (this.Header is not null)
            {
                yield return this.Header;
            }
        }
    }
}