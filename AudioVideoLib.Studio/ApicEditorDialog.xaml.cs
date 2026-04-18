namespace AudioVideoLib.Studio;

using System;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

using AudioVideoLib.Tags;

using Microsoft.Win32;

public partial class ApicEditorDialog : Window
{
    private byte[] _pictureData = [];
    private string _imageFormat = "image/jpeg";

    public ApicEditorDialog()
    {
        InitializeComponent();

        foreach (var value in Enum.GetValues<Id3v2AttachedPictureType>())
        {
            PictureTypeCombo.Items.Add(value);
        }

        PictureTypeCombo.SelectedItem = Id3v2AttachedPictureType.CoverFront;
    }

    public static bool Edit(Window owner, Id3v2AttachedPictureFrame frame)
    {
        var dlg = new ApicEditorDialog { Owner = owner };
        dlg.LoadFromFrame(frame);
        if (dlg.ShowDialog() != true)
        {
            return false;
        }

        dlg.ApplyTo(frame);
        return true;
    }

    private void LoadFromFrame(Id3v2AttachedPictureFrame frame)
    {
        _pictureData = frame.PictureData ?? [];
        _imageFormat = frame.ImageFormat ?? string.Empty;

        MimeBox.Text = _imageFormat;
        DescriptionBox.Text = frame.Description ?? string.Empty;
        PictureTypeCombo.SelectedItem = frame.PictureType;
        RefreshPreview();
    }

    private void ApplyTo(Id3v2AttachedPictureFrame frame)
    {
        frame.TextEncoding = Id3v2FrameEncodingType.UTF8;
        frame.ImageFormat = string.IsNullOrEmpty(MimeBox.Text) ? _imageFormat : MimeBox.Text;
        frame.PictureType = PictureTypeCombo.SelectedItem is Id3v2AttachedPictureType pt
            ? pt
            : Id3v2AttachedPictureType.CoverFront;
        frame.Description = DescriptionBox.Text ?? string.Empty;
        frame.PictureData = _pictureData;
    }

    private void LoadFromFile_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "Select image",
            Filter = "Image files|*.jpg;*.jpeg;*.png;*.gif;*.bmp|All files|*.*",
        };
        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            _pictureData = File.ReadAllBytes(dlg.FileName);
            _imageFormat = GuessMimeType(dlg.FileName);
            MimeBox.Text = _imageFormat;
            RefreshPreview();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not read image:\n\n{ex.Message}", "Load image",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ExportToFile_Click(object sender, RoutedEventArgs e)
    {
        if (_pictureData.Length == 0)
        {
            MessageBox.Show(this, "No image to export.", "Export picture", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var mime = (MimeBox.Text ?? _imageFormat ?? string.Empty).Trim().ToLowerInvariant();
        var extension = mime switch
        {
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/bmp" => ".bmp",
            "image/webp" => ".webp",
            _ => ".bin",
        };

        var dlg = new SaveFileDialog
        {
            Title = "Export picture",
            FileName = $"picture{extension}",
            Filter = $"{extension.TrimStart('.').ToUpperInvariant()} file|*{extension}|All files|*.*",
        };
        if (dlg.ShowDialog(this) != true)
        {
            return;
        }

        try
        {
            File.WriteAllBytes(dlg.FileName, _pictureData);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not write file:\n\n{ex.Message}", "Export picture",
                MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void ClearImage_Click(object sender, RoutedEventArgs e)
    {
        _pictureData = [];
        PreviewImage.Source = null;
        InfoText.Text = "No image.";
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private void RefreshPreview()
    {
        if (_pictureData.Length == 0)
        {
            PreviewImage.Source = null;
            InfoText.Text = "No image.";
            return;
        }

        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.StreamSource = new MemoryStream(_pictureData);
            bi.EndInit();
            bi.Freeze();
            PreviewImage.Source = bi;
            InfoText.Text = $"{bi.PixelWidth}×{bi.PixelHeight} • {_pictureData.Length:N0} bytes";
        }
        catch
        {
            PreviewImage.Source = null;
            InfoText.Text = $"(undecodable image, {_pictureData.Length:N0} bytes)";
        }
    }

    private static string GuessMimeType(string path)
    {
        return Path.GetExtension(path).ToLowerInvariant() switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png"            => "image/png",
            ".gif"            => "image/gif",
            ".bmp"            => "image/bmp",
            _                 => "application/octet-stream",
        };
    }
}
