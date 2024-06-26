﻿using Avalonia.Controls;
using AvaloniaEdit;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Utilities.Syntax;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace SkEditor.Views.Settings.Personalization;

public partial class FileSyntaxes : UserControl
{
    public FileSyntaxes()
    {
        InitializeComponent();

        AssignCommands();
        LoadSyntaxes();
    }

    private void LoadSyntaxes()
    {
        var syntaxes = SyntaxLoader.FileSyntaxes;
        var availableLangNames = syntaxes.Select(x => x.Config.LanguageName).Distinct().ToList();

        foreach (string langName in availableLangNames)
        {
            var selectedSyntax = ApiVault.Get().GetAppConfig().FileSyntaxes.FirstOrDefault(x => x.Key.Equals(langName)).Value ?? null;

            var expander = GenerateExpander(langName, selectedSyntax);
            SyntaxesStackPanel.Children.Add(expander);
        }
    }

    private SettingsExpander GenerateExpander(string language, string selectedSyntaxFullIdName)
    {
        var comboBox = new ComboBox { Name = language };
        var expander = new SettingsExpander
        {
            Header = language,
            IconSource = new SymbolIconSource { Symbol = Symbol.Code },
            Footer = comboBox
        };

        var fileSyntaxes = SyntaxLoader.FileSyntaxes.Where(x => x.Config.LanguageName.Equals(language)).ToList();

        foreach (var syntax in fileSyntaxes)
        {
            var newItem = new ComboBoxItem
            {
                Content = syntax.Config.SyntaxName,
                Tag = syntax.Config.FullIdName
            };
            comboBox.Items.Add(newItem);
            if (syntax.Config.FullIdName.Equals(selectedSyntaxFullIdName))
                comboBox.SelectedItem = newItem;
        }

        if (comboBox.SelectedItem == null)
            comboBox.SelectedIndex = 0;

        comboBox.SelectionChanged += (_, _) =>
        {
            var config = ApiVault.Get().GetAppConfig();
            var selectedFullIdName = (comboBox.SelectedValue as ComboBoxItem).Tag.ToString();
            var selectedFileSyntax = SyntaxLoader.FileSyntaxes.FirstOrDefault(x => x.Config.FullIdName.Equals(selectedFullIdName));

            config.FileSyntaxes[selectedFileSyntax.Config.LanguageName] = selectedFileSyntax.Config.FullIdName;

            List<TabViewItem> tabs = ApiVault.Get().GetTabView().TabItems
                .OfType<TabViewItem>()
                .Where(tab => tab.Content is TextEditor)
                .Where(tab =>
                {
                    var ext = Path.GetExtension(tab.Tag?.ToString()?.ToLower() ?? "");
                    return tab.Tag is string &&
                           selectedFileSyntax.Config.Extensions.Contains(ext);
                })
                .ToList();

            foreach (var tab in tabs)
            {
                var editor = tab.Content as TextEditor;
                editor.SyntaxHighlighting = selectedFileSyntax.Highlighting;
            }
        };

        return expander;
    }

    private void AssignCommands()
    {
        Title.BackButton.Command = new RelayCommand(() => SettingsWindow.NavigateToPage(typeof(PersonalizationPage)));
    }
}