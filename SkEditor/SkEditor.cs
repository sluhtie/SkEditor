﻿using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using AvaloniaEdit;
using FluentAvalonia.UI.Controls;
using SkEditor.API;
using SkEditor.Utilities;
using SkEditor.Views;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace SkEditor;

public class SkEditor : ISkEditorAPI
{
	private string[] startupFiles = null;
	public MainWindow mainWindow;
	private AppConfig appConfig;

	public SkEditor(string[] args)
	{
		startupFiles = args.Where(File.Exists).ToArray();

		ApiVault.Set(this);
	}

	/// <returns>Main window</returns>
	public MainWindow GetMainWindow()
	{
		return mainWindow;
	}

	/// <returns>Startup files</returns>
	public string[] GetStartupFiles()
	{
		return startupFiles;
	}


	/// <returns>App's main menu</returns>
	public Menu GetMenu()
	{
		return GetMainWindow().MainMenu.MainMenu;
	}

	/// <returns>True if file is currently opened</returns>
	public bool IsFileOpen()
	{
		return GetTextEditor() != null;
	}

	/// <returns>True if provided TabItem is file</returns>
	public bool IsFile(TabViewItem tabItem)
	{
		return tabItem.Content is TextEditor;
	}


	/// <returns>Current opened text editor if exists, otherwise null</returns>
	public TextEditor GetTextEditor()
	{
		return GetMainWindow().TabControl.SelectedItem is TabViewItem tabItem && IsFile(tabItem) ? tabItem.Content as TextEditor : null;
	}

	/// <returns>App's tabcontrol</returns>
	public TabView GetTabView()
	{
		return GetMainWindow().TabControl;
	}

	public AppConfig GetAppConfig() => appConfig ??= AppConfig.Load().Result;

	/// <summary>
	/// Opens provided URL in default browser
	/// </summary>
	public void OpenUrl(string url)
	{
		try
		{
			Process.Start(new ProcessStartInfo
			{
				FileName = url,
				UseShellExecute = true
			});
		}
		catch
		{
			OpenUrl(url.Replace("&", "^&"));
		}
	}


	/// <summary>
	/// Shows message box with provided message and title on provided window
	/// </summary>
	public async void ShowMessage(string title, string message, Window window)
	{
		var dialog = new ContentDialog()
		{
			Title = title,
			Content = message,
			CloseButtonText = Translation.Get("CloseButton")
		};
		await dialog.ShowAsync(window);
	}
	/// <summary>
	/// Shows message box with provided message and title
	/// </summary>
	public void ShowMessage(string title, string message)
	{
		ShowMessage(title, message, GetTopWindow());
	}


	/// <summary>
	/// Shows info box with provided message and title
	/// </summary>
	public async Task<ContentDialogResult> ShowMessageWithIcon(string title, string message, IconSource icon, string iconColor = "#ffffff", string primaryButtonContent = "ConfirmButton", string closeButtonContent = "CancelButton", bool primaryButton = true)
	{
		var dialog = new ContentDialog()
		{
			Title = title,
			CloseButtonText = Translation.Get(closeButtonContent),
		};

		if (primaryButton) dialog.PrimaryButtonText = Translation.Get(primaryButtonContent);

		if (icon is SymbolIconSource symbolIcon) symbolIcon.FontSize = 36;

		IconSourceElement iconElement = new()
		{
			IconSource = icon,
			Width = 36,
			Height = 36,
		};

		var grid = new Grid();
		grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Auto) });
		grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });

		var textBlock = new TextBlock()
		{
			Text = message,
			FontSize = 16,
			Margin = new Thickness(10, 10, 0, 0),
			TextWrapping = TextWrapping.Wrap,
			MaxWidth = 400,
		};

		Grid.SetColumn(iconElement, 0);
		Grid.SetColumn(textBlock, 1);

		grid.Children.Add(iconElement);
		grid.Children.Add(textBlock);

		dialog.Content = grid;

		return await dialog.ShowAsync(GetTopWindow());
	}

	public void Debug(string message)
	{
		ShowMessage("Debug", message, GetTopWindow());
	}

	private Window GetTopWindow()
	{
		var windows = ((IClassicDesktopStyleApplicationLifetime)Application.Current.ApplicationLifetime).Windows;
		var dialog = windows.Where(x => !x.OwnedWindows.Any()).FirstOrDefault();
		dialog ??= GetMainWindow();
		return dialog;
	}

	public void Log(string message)
	{
		Serilog.Log.Information(message);
		GetMainWindow().BottomBar.UpdateLogs(message);
	}

	public bool IsAddonEnabled(string addonName) => AddonLoader.Addons.Any(x => x.Name.Equals(addonName));



	#region Events

	public event EventHandler<TextEditorEventArgs> FileCreated;
	public void OnFileCreated(TextEditor textEditor) => FileCreated?.Invoke(this, new TextEditorEventArgs(textEditor));

	/// <summary>
	/// Returns true if file can be closed
	/// </summary>
	/// <returns>False if file closing was cancelled</returns>
	public event EventHandler<TextEditorCancelEventArgs> FileClosing;
	public bool OnFileClosing(TextEditor textEditor)
	{
		TextEditorCancelEventArgs args = new(textEditor);
		FileClosing?.Invoke(this, args);
		return !args.Cancel;
	}

	public event EventHandler SettingsOpened;
	public void OnSettingsOpened() => SettingsOpened?.Invoke(this, EventArgs.Empty);

	#endregion
}


public class TextEditorEventArgs(TextEditor textEditor) : EventArgs
{
	public TextEditor TextEditor { get; } = textEditor;
}

public class TextEditorCancelEventArgs(TextEditor textEditor) : EventArgs
{
	public TextEditor TextEditor { get; } = textEditor;
	public bool Cancel { get; set; }
}