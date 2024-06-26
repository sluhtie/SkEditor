using Avalonia.Controls;
using CommunityToolkit.Mvvm.Input;
using FluentAvalonia.UI.Windowing;
using SkEditor.API;
using SkEditor.Utilities;
using SkEditor.Utilities.Styling;

namespace SkEditor.Views;
public partial class PublishWindow : AppWindow
{
    private string CurrentService => (WebsiteComboBox.SelectedItem as ComboBoxItem).Content as string;


    public PublishWindow()
    {
        InitializeComponent();
        InitializeUI();
    }

    private void InitializeUI()
    {
        WindowStyler.Style(this);
        TitleBar.ExtendsContentIntoTitleBar = false;

        PublishButton.Command = new RelayCommand(Publish);

        CopyButton.Command = new RelayCommand(async () => await Clipboard.SetTextAsync(ResultTextBox.Text));

        WebsiteComboBox.SelectedIndex = ApiVault.Get().GetAppConfig().LastUsedPublishService switch
        {
            "Pastebin" => 0,
            "code.skript.pl" => 1,
            "skUnity Parser" => 2,
            _ => 0,
        };

        WebsiteComboBox.SelectionChanged += (sender, e) => UpdateServiceInfo();

        UpdateServiceInfo();
    }

    private void UpdateServiceInfo()
    {
        AppConfig appConfig = ApiVault.Get().GetAppConfig();
        ApiKeyTextBox.Text = CurrentService switch
        {
            "Pastebin" => appConfig.PastebinApiKey,
            "code.skript.pl" => appConfig.CodeSkriptPlApiKey,
            "skUnity Parser" => appConfig.SkUnityAPIKey,
            _ => "",
        };

        appConfig.LastUsedPublishService = CurrentService;

        AnonymouslyCheckBox.IsVisible = LanguageComboBox.IsVisible = CurrentService.Equals("code.skript.pl");
    }

    private void SaveApiKey()
    {
        switch (CurrentService)
        {
            case "Pastebin":
                ApiVault.Get().GetAppConfig().PastebinApiKey = ApiKeyTextBox.Text;
                break;
            case "code.skript.pl":
                ApiVault.Get().GetAppConfig().CodeSkriptPlApiKey = ApiKeyTextBox.Text;
                break;
            case "skUnity Parser":
                ApiVault.Get().GetAppConfig().SkUnityAPIKey = ApiKeyTextBox.Text;
                break;
        }
    }

    private void OpenSiteWithApiKey()
    {
        string url = CurrentService switch
        {
            "code.skript.pl" => "https://code.skript.pl/api-key",
            "skUnity Parser" => "https://skunity.com/dashboard/skunity-api",
            _ => "https://pastebin.com/doc_api"
        };

        ApiVault.Get().OpenUrl(url);
    }

    private void Publish()
    {
        string code = ApiVault.Get().GetTextEditor().Document.Text;

        if (string.IsNullOrWhiteSpace(code))
        {
            ApiVault.Get().ShowMessage("Error", "You can't publish empty code!", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(ApiKeyTextBox.Text))
        {
            ApiVault.Get().ShowMessage("Error", "You need to enter the API key!", this);
            return;
        }

        switch ((WebsiteComboBox.SelectedItem as ComboBoxItem).Content as string)
        {
            case "Pastebin":
                CodePublisher.PublishPastebin(code, this);
                break;
            case "code.skript.pl":
                CodePublisher.PublishCodeSkriptPl(code, this);
                break;
            case "skUnity Parser":
                CodePublisher.PublishSkUnity(code, this);
                break;
        }
    }
}
