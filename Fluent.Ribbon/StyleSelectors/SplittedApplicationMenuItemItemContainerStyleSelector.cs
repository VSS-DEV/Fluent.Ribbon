﻿namespace Fluent.StyleSelectors;

using System.Windows;
using System.Windows.Controls;
using MenuItem = Fluent.MenuItem;

/// <summary>
/// <see cref="StyleSelector"/> for <see cref="ItemsControl.ItemContainerStyle"/> in <see cref="MenuItem"/> with style SplitedApplicationMenuItem.
/// </summary>
public class SplitApplicationMenuItemItemContainerStyleSelector : StyleSelector
{
    /// <summary>
    ///     A singleton instance for <see cref="SplitApplicationMenuItemItemContainerStyleSelector" />.
    /// </summary>
    public static SplitApplicationMenuItemItemContainerStyleSelector Instance { get; } = new();

    /// <inheritdoc />
    public override Style? SelectStyle(object item, DependencyObject container)
    {
        switch (item)
        {
            case MenuItem _:
                return (container as FrameworkElement)?.TryFindResource("Fluent.Ribbon.Styles.ApplicationMenu.MenuItemSecondLevel") as Style;
        }

        return base.SelectStyle(item, container);
    }
}