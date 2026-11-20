using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Markup;
using System.Windows.Media;

namespace NghiLe
{
    internal class PanelPage : Grid
    {
        internal static readonly Lazy<ResourceDictionary> Theme = new Lazy<ResourceDictionary>(() =>
        {
            using (var stream = typeof(PanelPage).Assembly.GetManifestResourceStream("NghiLe.PanelTheme"))
                return (ResourceDictionary)XamlReader.Load(stream);
        });

        internal PanelPage(string title)
        {
            Resources.MergedDictionaries.Add(Theme.Value);
            TextElement.SetForeground(this, (Brush)FindResource("PanelInk"));
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            UseLayoutRounding = true;
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition());
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            Children.Add(SectionHeading(title, this, 0));
        }

        internal void SetContent(UIElement body, FrameworkElement footer)
        {
            SetRow(body, 1);
            Children.Add(body);
            footer.Margin = new Thickness(0, 8, 0, 0);
            SetRow(footer, 2);
            Children.Add(footer);
        }

        internal static ScrollViewer Scroll(UIElement content)
        {
            return new ScrollViewer { Content = content, Focusable = false, PanningMode = PanningMode.VerticalOnly,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        }

        internal static DockPanel SectionHeading(string title, FrameworkElement resources, double topMargin)
        {
            var heading = new DockPanel { Margin = new Thickness(0, topMargin, 0, 5) };
            heading.Children.Add(new TextBlock { Text = title, FontSize = 18, FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)resources.FindResource("PanelGold"), Margin = new Thickness(0, 0, 12, 0) });
            heading.Children.Add(new Border { Height = 1, VerticalAlignment = VerticalAlignment.Center,
                Background = (Brush)resources.FindResource("PanelDivider") });
            return heading;
        }
    }
}
