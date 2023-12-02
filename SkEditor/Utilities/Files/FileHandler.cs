﻿using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using AvaloniaEdit;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Controls;
using FluentAvalonia.UI.Windowing;
using SkEditor.API;
using SkEditor.Utilities.Editor;
using SkEditor.Utilities.Syntax;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SkEditor.Utilities.Files;
public class FileHandler
{
	public static Regex RegexPattern => new(Translation.Get("NewFileNameFormat").Replace("{0}", @"[0-9]+"));

	public static Action<AppWindow, DragEventArgs> FileDropAction = (window, e) =>
	{
		try
		{
			e.Data.GetFiles().Where(f => !Directory.Exists(f.Path.AbsolutePath)).ToList().ForEach(file =>
			{
				OpenFile(file.Path.AbsolutePath);
			});
		}
		catch { }
	};

	private static int GetUntitledNumber() => (ApiVault.Get().GetTabView().TabItems as IList).Cast<TabViewItem>().Count(tab => RegexPattern.IsMatch(tab.Header.ToString())) + 1;

	public static void NewFile()
	{
		string header = Translation.Get("NewFileNameFormat").Replace("{0}", GetUntitledNumber().ToString());
		TabViewItem tabItem = FileBuilder.Build(header);
		(ApiVault.Get().GetTabView().TabItems as IList)?.Add(tabItem);
	}

	public async static void OpenFile()
	{
		bool untitledFileOpen = ApiVault.Get().GetTabView().TabItems.Count() == 1 &&
				ApiVault.Get().GetTextEditor().Text.Length == 0 &&
				ApiVault.Get().GetTabView().SelectedItem is TabViewItem item &&
				item.Header.ToString().Contains(Translation.Get("NewFileNameFormat").Replace("{0}", "")) &&
				!item.Header.ToString().EndsWith('*');

		var topLevel = TopLevel.GetTopLevel(ApiVault.Get().GetMainWindow());

		var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
		{
			Title = Translation.Get("WindowTitleOpenFilePicker"),
			AllowMultiple = true
		});

		files.ToList().ForEach(file => OpenFile(file.Path.AbsolutePath));
		if (untitledFileOpen) await CloseFile((ApiVault.Get().GetTabView().TabItems as IList)[0] as TabViewItem);
	}

	public static void OpenFile(string path)
	{
		TabViewItem tabItem = FileBuilder.Build(Path.GetFileName(path), path);
		(ApiVault.Get().GetTabView().TabItems as IList)?.Add(tabItem);
	}

	public async static void SaveFile()
	{
		if (!ApiVault.Get().IsFileOpen()) return;

		TabViewItem item = ApiVault.Get().GetTabView().SelectedItem as TabViewItem;
		string path = item.Tag.ToString();

		if (string.IsNullOrEmpty(path))
		{
			SaveAsFile();
			return;
		}

		string textToWrite = ApiVault.Get().GetTextEditor().Text;
		using StreamWriter writer = new(path, false);
		await writer.WriteAsync(textToWrite);

		if (!item.Header.ToString().EndsWith('*')) return;
		item.Header = item.Header.ToString()[..^1];
	}

	public async static void SaveAsFile()
	{
		if (!ApiVault.Get().IsFileOpen()) return;

		var topLevel = TopLevel.GetTopLevel(ApiVault.Get().GetMainWindow());
		var tabView = ApiVault.Get().GetTabView();

		if (tabView.SelectedItem is not TabViewItem item) return;

		string header = item.Header.ToString().TrimEnd('*');
		string itemTag = item.Tag.ToString();
		IStorageFolder suggestedFolder = string.IsNullOrEmpty(itemTag)
			? await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents)
			: await topLevel.StorageProvider.TryGetFolderFromPathAsync(itemTag);

		FilePickerSaveOptions saveOptions = new()
		{
			Title = Translation.Get("WindowTitleSaveFilePicker"),
			SuggestedFileName = header,
		};
		if (suggestedFolder is not null) saveOptions.SuggestedStartLocation = suggestedFolder;

		var file = await topLevel.StorageProvider.SaveFilePickerAsync(saveOptions);

		if (file is null) return;

		string absolutePath = Uri.UnescapeDataString(file.Path.AbsolutePath);

		Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
		using var stream = File.OpenWrite(absolutePath);
		ApiVault.Get().GetTextEditor().Save(stream);

		item.Header = file.Name;
		item.Tag = absolutePath;

		SyntaxLoader.SetSyntax(ApiVault.Get().GetTextEditor(), absolutePath);
		IconSetter.SetIcon(item);
		ToolTip toolTip = new()
		{
			Content = absolutePath,
		};
		ToolTip.SetTip(item, toolTip);
	}

	public async static void CloseFile(TabViewTabCloseRequestedEventArgs e) => await CloseFile(e.Tab);
	public async static void CloseCurrentFile() => await CloseFile(ApiVault.Get().GetTabView().SelectedItem as TabViewItem);

	public async static void CloseAllFiles()
	{
		ContentDialogResult result = await ApiVault.Get().ShowMessageWithIcon(Translation.Get("Attention"), Translation.Get("ClosingAllFiles"), new SymbolIconSource() { Symbol = Symbol.ImportantFilled });
		if (result != ContentDialogResult.Primary) return;

		List<TabViewItem> tabItems = (ApiVault.Get().GetTabView().TabItems as IList)?.Cast<TabViewItem>().ToList();
		tabItems.ForEach(DisposeEditorData);
		tabItems.Clear();
		NewFile();
	}

	public static async Task CloseFile(TabViewItem item)
	{
		if (item.Content is TextEditor editor && !ApiVault.Get().OnFileClosing(editor)) return;

		DisposeEditorData(item);

		string header = item.Header.ToString();

		if (header.EndsWith('*'))
		{
			ContentDialogResult result = await ApiVault.Get().ShowMessageWithIcon(Translation.Get("Attention"), Translation.Get("ClosingUnsavedFile"), new SymbolIconSource() { Symbol = Symbol.ImportantFilled });
			if (result != ContentDialogResult.Primary) return;
		}

		var tabView = ApiVault.Get().GetTabView();
		var tabItems = tabView.TabItems as IList;

		tabItems?.Remove(item);

		if (tabItems.Count == 0) NewFile();
	}

	private static void DisposeEditorData(TabViewItem item)
	{
		if (item.Content is not TextEditor editor) return;

		TextEditorEventHandler.ScrollViewers.Remove(editor);
	}

	public static void SwitchTab(int index)
	{
		var tabView = ApiVault.Get().GetTabView();
		if (index < tabView.TabItems.Count()) tabView.SelectedIndex = index;
	}
}
