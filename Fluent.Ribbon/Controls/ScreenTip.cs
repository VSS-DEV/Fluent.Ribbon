﻿// ReSharper disable once CheckNamespace
namespace Fluent;

using System;
using System.Collections;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Fluent.Helpers;
using Fluent.Internal.KnownBoxes;

/// <summary>
/// ScreenTips display the name of the control,
/// the keyboard shortcut for the control, and a brief description
/// of how to use the control. ScreenTips also can provide F1 support,
/// which opens help and takes the user directly to the related
/// help topic for the control whose ScreenTip was
/// displayed when the F1 button was pressed
/// </summary>
public class ScreenTip : ToolTip, ILogicalChildSupport
{
    #region Initialization

    /// <summary>
    /// Static constructor
    /// </summary>
    static ScreenTip()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ScreenTip), new FrameworkPropertyMetadata(typeof(ScreenTip)));
    }

    /// <summary>
    /// Default constructor
    /// </summary>
    public ScreenTip()
    {
        this.Opened += this.OnToolTipOpened;
        this.Closed += this.OnToolTipClosed;
        this.CustomPopupPlacementCallback = this.CustomPopupPlacementMethod;
        this.Placement = PlacementMode.Custom;
        this.HelpLabelVisibility = Visibility.Visible;
    }

    #endregion

    #region Popup Custom Placement

    // Calculate two variants: below and upper ribbon
    private CustomPopupPlacement[] CustomPopupPlacementMethod(Size popupSize, Size targetSize, Point offset)
    {
        if (this.PlacementTarget is null)
        {
            return Array.Empty<CustomPopupPlacement>();
        }

        Ribbon? ribbon = null;
        UIElement? topLevelElement = null;
        FindControls(this.PlacementTarget, ref ribbon, ref topLevelElement);

        var dpiScale = VisualTreeHelper.GetDpi(this.PlacementTarget);

        // Exclude QAT items
        var notQuickAccessItem = !IsQuickAccessItem(this.PlacementTarget);
        var notContextMenuChild = !IsContextMenuChild(this.PlacementTarget);
        var rightToLeftOffset = this.FlowDirection == FlowDirection.RightToLeft
            ? -popupSize.Width
            : 0;
        var rightToLeftOffsetScaled = rightToLeftOffset * dpiScale.DpiScaleX;

        var decoratorChild = GetDecoratorChild(topLevelElement);

        if (notQuickAccessItem
            && this.IsRibbonAligned
            && ribbon is not null)
        {
            var belowY = ribbon.TranslatePoint(new Point(0, ribbon.ActualHeight), this.PlacementTarget).Y;
            belowY *= dpiScale.DpiScaleY;
            var aboveY = (ribbon.TranslatePoint(new Point(0, 0), this.PlacementTarget).Y * dpiScale.DpiScaleY) - popupSize.Height;

            var below = new CustomPopupPlacement(new Point(rightToLeftOffsetScaled, belowY + 1), PopupPrimaryAxis.Horizontal);
            var above = new CustomPopupPlacement(new Point(rightToLeftOffsetScaled, aboveY - 1), PopupPrimaryAxis.Horizontal);
            return new[] { below, above };
        }

        if (notQuickAccessItem
            && this.IsRibbonAligned
            && notContextMenuChild
            && topLevelElement is Window == false
            && decoratorChild is not null)
        {
            // Placed on Popup?
            var belowY = decoratorChild.TranslatePoint(new Point(0, ((FrameworkElement)decoratorChild).ActualHeight), this.PlacementTarget).Y;
            belowY *= dpiScale.DpiScaleY;
            var aboveY = (decoratorChild.TranslatePoint(new Point(0, 0), this.PlacementTarget).Y * dpiScale.DpiScaleY) - popupSize.Height;

            var below = new CustomPopupPlacement(new Point(rightToLeftOffsetScaled, belowY + 1), PopupPrimaryAxis.Horizontal);
            var above = new CustomPopupPlacement(new Point(rightToLeftOffsetScaled, aboveY - 1), PopupPrimaryAxis.Horizontal);
            return new[] { below, above };
        }

        return new[]
        {
            new CustomPopupPlacement(new Point(rightToLeftOffsetScaled, (this.PlacementTarget.RenderSize.Height + 1) * dpiScale.DpiScaleY), PopupPrimaryAxis.Horizontal),
            new CustomPopupPlacement(new Point(rightToLeftOffsetScaled, -popupSize.Height - 1), PopupPrimaryAxis.Horizontal)
        };
    }

    private static bool IsContextMenuChild(UIElement element)
    {
        var currentElement = element;
        do
        {
            var parent = VisualTreeHelper.GetParent(currentElement) as UIElement;

            if (parent is System.Windows.Controls.ContextMenu)
            {
                return true;
            }

            currentElement = parent;
        }
        while (currentElement is not null);

        return false;
    }

    private static bool IsQuickAccessItem(UIElement element)
    {
        var currentElement = element;
        do
        {
            var parent = VisualTreeHelper.GetParent(currentElement) as UIElement;
            if (parent is QuickAccessToolBar)
            {
                return true;
            }

            currentElement = parent;
        }
        while (currentElement is not null);

        return false;
    }

    private static UIElement? GetDecoratorChild(UIElement? popupRoot)
    {
        switch (popupRoot)
        {
            case null:
                return null;

            case AdornerDecorator decorator:
                return decorator.Child;
        }

        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(popupRoot); i++)
        {
            var element = GetDecoratorChild(VisualTreeHelper.GetChild(popupRoot, i) as UIElement);
            if (element is not null)
            {
                return element;
            }
        }

        return null;
    }

    private static void FindControls(UIElement? obj, ref Ribbon? ribbon, ref UIElement? topLevelElement)
    {
        if (obj is null)
        {
            return;
        }

        ribbon ??= obj as Ribbon;

        var parentVisual = VisualTreeHelper.GetParent(obj) as UIElement;
        if (parentVisual is null)
        {
            topLevelElement = obj;
        }
        else
        {
            FindControls(parentVisual, ref ribbon, ref topLevelElement);
        }
    }

    #endregion

    #region Title Property

    /// <summary>
    /// Gets or sets title of the screen tip
    /// </summary>
    [System.ComponentModel.DisplayName("Title")]
    [System.ComponentModel.Category("Screen Tip")]
    [System.ComponentModel.Description("Title of the screen tip")]
    public string Title
    {
        get => (string)this.GetValue(TitleProperty);
        set => this.SetValue(TitleProperty, value);
    }

    /// <summary>Identifies the <see cref="Title"/> dependency property.</summary>
    public static readonly DependencyProperty TitleProperty =
#pragma warning disable WPF0010 // Default value type must match registered type.
        DependencyProperty.Register(nameof(Title), typeof(string), typeof(ScreenTip), new PropertyMetadata(StringBoxes.Empty));
#pragma warning restore WPF0010 // Default value type must match registered type.

    #endregion

    #region Text Property

    /// <summary>
    /// Gets or sets text of the screen tip
    /// </summary>
    [System.ComponentModel.DisplayName("Text")]
    [System.ComponentModel.Category("Screen Tip")]
    [System.ComponentModel.Description("Main text of the screen tip")]
    public object? Text
    {
        get => (object?)this.GetValue(TextProperty);
        set => this.SetValue(TextProperty, value);
    }

    /// <summary>Identifies the <see cref="Text"/> dependency property.</summary>
    public static readonly DependencyProperty TextProperty =
#pragma warning disable WPF0010 // Default value type must match registered type.
        DependencyProperty.Register(nameof(Text), typeof(object), typeof(ScreenTip), new PropertyMetadata(StringBoxes.Empty, LogicalChildSupportHelper.OnLogicalChildPropertyChanged));
#pragma warning restore WPF0010 // Default value type must match registered type.

    #endregion

    /// <summary>Identifies the <see cref="TextTemplate"/> dependency property.</summary>
    public static readonly DependencyProperty TextTemplateProperty = DependencyProperty.Register(
        nameof(TextTemplate), typeof(DataTemplate), typeof(ScreenTip), new PropertyMetadata(default(DataTemplate)));

    /// <summary>
    /// Defines the <see cref="DataTemplate"/> being used to render <see cref="Text"/>.
    /// </summary>
    public DataTemplate? TextTemplate
    {
        get => (DataTemplate?)this.GetValue(TextTemplateProperty);
        set => this.SetValue(TextTemplateProperty, value);
    }

    #region DisableReason Property

    /// <summary>
    /// Gets or sets disable reason of the associated screen tip's control
    /// </summary>
    [System.ComponentModel.DisplayName("Disable Reason")]
    [System.ComponentModel.Category("Screen Tip")]
    [System.ComponentModel.Description("Describe here what would cause disable of the control")]
    public string DisableReason
    {
        get => (string)this.GetValue(DisableReasonProperty);
        set => this.SetValue(DisableReasonProperty, value);
    }

    /// <summary>Identifies the <see cref="DisableReason"/> dependency property.</summary>
    public static readonly DependencyProperty DisableReasonProperty =
#pragma warning disable WPF0010 // Default value type must match registered type.
        DependencyProperty.Register(nameof(DisableReason), typeof(string), typeof(ScreenTip), new PropertyMetadata(StringBoxes.Empty));
#pragma warning restore WPF0010 // Default value type must match registered type.

    #endregion

    #region HelpTopic Property

    /// <summary>
    /// Gets or sets help topic of the ScreenTip
    /// </summary>
    [System.ComponentModel.DisplayName("Help Topic")]
    [System.ComponentModel.Category("Screen Tip")]
    [System.ComponentModel.Description("Help topic (it will be used to execute help)")]
    public object? HelpTopic
    {
        get => this.GetValue(HelpTopicProperty);
        set => this.SetValue(HelpTopicProperty, value);
    }

    /// <summary>Identifies the <see cref="HelpTopic"/> dependency property.</summary>
    public static readonly DependencyProperty HelpTopicProperty =
        DependencyProperty.Register(nameof(HelpTopic), typeof(object), typeof(ScreenTip), new PropertyMetadata(LogicalChildSupportHelper.OnLogicalChildPropertyChanged));

    #endregion

    #region Image Property

    /// <summary>
    /// Gets or sets image of the screen tip
    /// </summary>
    [System.ComponentModel.DisplayName("Image")]
    [System.ComponentModel.Category("Screen Tip")]
    [System.ComponentModel.Description("Image of the screen tip")]
    public object? Image
    {
        get => (object?)this.GetValue(ImageProperty);
        set => this.SetValue(ImageProperty, value);
    }

    /// <summary>Identifies the <see cref="Image"/> dependency property.</summary>
    public static readonly DependencyProperty ImageProperty =
        DependencyProperty.Register(nameof(Image), typeof(object), typeof(ScreenTip), new PropertyMetadata(LogicalChildSupportHelper.OnLogicalChildPropertyChanged));

    #endregion

    #region ShowHelp Property

    /// <summary>
    /// Shows or hides the Help Label
    /// </summary>
    [System.ComponentModel.DisplayName("HelpLabelVisibility")]
    [System.ComponentModel.Category("Screen Tip")]
    [System.ComponentModel.Description("Sets the visibility of the F1 Help Label")]
    public Visibility HelpLabelVisibility
    {
        get => (Visibility)this.GetValue(HelpLabelVisibilityProperty);
        set => this.SetValue(HelpLabelVisibilityProperty, VisibilityBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="HelpLabelVisibility"/> dependency property.</summary>
    public static readonly DependencyProperty HelpLabelVisibilityProperty =
        DependencyProperty.Register(nameof(HelpLabelVisibility), typeof(Visibility), typeof(ScreenTip), new PropertyMetadata(VisibilityBoxes.Visible));
    #endregion

    #region Help Invocation

    /// <summary>
    /// Occurs when user press F1 on ScreenTip with HelpTopic filled
    /// </summary>
    public static event EventHandler<ScreenTipHelpEventArgs>? HelpPressed;

    #endregion

    #region IsRibbonAligned

    /// <summary>
    /// Gets or set whether ScreenTip should positioned below Ribbon
    /// </summary>
    public bool IsRibbonAligned
    {
        get => (bool)this.GetValue(IsRibbonAlignedProperty);
        set => this.SetValue(IsRibbonAlignedProperty, BooleanBoxes.Box(value));
    }

    /// <summary>Identifies the <see cref="IsRibbonAligned"/> dependency property.</summary>
    public static readonly DependencyProperty IsRibbonAlignedProperty =
        DependencyProperty.Register(nameof(IsRibbonAligned), typeof(bool), typeof(ScreenTip),
            new PropertyMetadata(BooleanBoxes.TrueBox));

    #endregion

    #region F1 Help Handling

    // Currently focused element
    private IInputElement? focusedElement;

    private void OnToolTipClosed(object sender, RoutedEventArgs e)
    {
        if (this.focusedElement is null)
        {
            return;
        }

        this.focusedElement.PreviewKeyDown -= this.OnFocusedElementPreviewKeyDown;
        this.focusedElement = null;
    }

    private void OnToolTipOpened(object sender, RoutedEventArgs e)
    {
        if (this.HelpTopic is null)
        {
            return;
        }

        this.focusedElement = Keyboard.FocusedElement;
        if (this.focusedElement is not null)
        {
            this.focusedElement.PreviewKeyDown += this.OnFocusedElementPreviewKeyDown;
        }
    }

    private void OnFocusedElementPreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F1)
        {
            return;
        }

        e.Handled = true;

        HelpPressed?.Invoke(null, new ScreenTipHelpEventArgs(this.HelpTopic));
    }

    #endregion

    /// <inheritdoc />
    protected override AutomationPeer OnCreateAutomationPeer() => new Automation.Peers.RibbonScreenTipAutomationPeer(this);

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

            if (this.Text is not null)
            {
                yield return this.Text;
            }

            if (this.Image is not null)
            {
                yield return this.Image;
            }

            if (this.HelpTopic is not null)
            {
                yield return this.HelpTopic;
            }
        }
    }
}

/// <summary>
/// Event args for HelpPressed event handler
/// </summary>
public class ScreenTipHelpEventArgs : EventArgs
{
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="helpTopic">Help topic</param>
    public ScreenTipHelpEventArgs(object? helpTopic)
    {
        this.HelpTopic = helpTopic;
    }

    /// <summary>
    /// Gets help topic associated with screen tip
    /// </summary>
    public object? HelpTopic { get; }
}