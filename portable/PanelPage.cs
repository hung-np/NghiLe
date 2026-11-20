using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
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

        internal readonly Button BackButton;

        internal PanelPage(string title, string backLabel, Action back)
        {
            Resources.MergedDictionaries.Add(Theme.Value);
            TextElement.SetForeground(this, (Brush)FindResource("PanelInk"));
            KeyboardNavigation.SetTabNavigation(this, KeyboardNavigationMode.Cycle);
            Margin = new Thickness(5);
            MaxWidth = 285;
            TextOptions.SetTextFormattingMode(this, TextFormattingMode.Display);
            UseLayoutRounding = true;
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            RowDefinitions.Add(new RowDefinition());
            RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var header = new DockPanel { Margin = new Thickness(0, 0, 0, 5) };
            BackButton = new Button { Content = "‹", ToolTip = backLabel, Width = 25, FontSize = 18,
                Padding = new Thickness(0), Margin = new Thickness(0, 0, 7, 0) };
            AutomationProperties.SetName(BackButton, backLabel);
            BackButton.Click += (s, e) => back();
            header.Children.Add(BackButton);
            var heading = new TextBlock { Text = title, FontSize = 16, FontWeight = FontWeights.SemiBold,
                VerticalAlignment = VerticalAlignment.Center };
            header.Children.Add(heading);
            Children.Add(header);
        }

        internal void SetContent(UIElement body, FrameworkElement footer)
        {
            SetRow(body, 1);
            Children.Add(body);
            footer.Margin = new Thickness(0, 5, 0, 0);
            SetRow(footer, 2);
            Children.Add(footer);
        }
    }
}
