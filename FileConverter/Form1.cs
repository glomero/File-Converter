using System;
using System.Windows.Forms;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using NAudio.Wave;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using SDImage = System.Drawing.Image;

namespace FileConverter;

public partial class MainForm : Form
{
    private string selectedFilePath = string.Empty;

    public MainForm()
    {
        InitializeComponent();
        InitializeUI();
    }

    private void InitializeUI()
    {
        // Initialize conversion types
        comboBoxConversion.Items.Clear();
        comboBoxConversion.Items.AddRange(new string[] {
            "PDF to DOCX",
            "JPG to PNG",
            "MP3 to WAV"
        });
        comboBoxConversion.SelectedIndex = 0;
    }

    private void btnSelectFile_Click(object sender, EventArgs e)
    {
        using var openFileDialog = new OpenFileDialog();
        string filter = comboBoxConversion.SelectedItem?.ToString() switch
        {
            "PDF to DOCX" => "PDF files (*.pdf)|*.pdf",
            "JPG to PNG" => "JPEG files (*.jpg;*.jpeg)|*.jpg;*.jpeg",
            "MP3 to WAV" => "MP3 files (*.mp3)|*.mp3",
            _ => "All files (*.*)|*.*"
        };
        
        openFileDialog.Filter = filter;

        if (openFileDialog.ShowDialog() == DialogResult.OK)
        {
            selectedFilePath = openFileDialog.FileName;
            lblFilePath.Text = $"Selected File: {Path.GetFileName(selectedFilePath)}";
        }
    }

    private async void btnConvert_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(selectedFilePath))
        {
            MessageBox.Show("Please select a file first.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        string? conversionType = comboBoxConversion.SelectedItem?.ToString();
        if (string.IsNullOrEmpty(conversionType))
        {
            MessageBox.Show("Please select a conversion type.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        try
        {
            progressBar.Value = 0;
            progressBar.Visible = true;
            btnConvert.Enabled = false;
            btnSelectFile.Enabled = false;

            await Task.Run(() =>
            {
                switch (conversionType)
                {
                    case "PDF to DOCX":
                        ConvertPdfToDocx(selectedFilePath);
                        break;
                    case "JPG to PNG":
                        ConvertJpgToPng(selectedFilePath);
                        break;
                    case "MP3 to WAV":
                        ConvertMp3ToWav(selectedFilePath);
                        break;
                    default:
                        throw new NotSupportedException("Unsupported conversion type.");
                }
            });

            progressBar.Value = 100;
            MessageBox.Show("Conversion completed successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error during conversion: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            progressBar.Visible = false;
            btnConvert.Enabled = true;
            btnSelectFile.Enabled = true;
        }
    }

    private void ConvertPdfToDocx(string filePath)
    {
        string outputPath = Path.ChangeExtension(filePath, ".docx");
        using var pdfReader = new PdfReader(filePath);
        using var pdfDocument = new PdfDocument(pdfReader);
        using var writer = new StreamWriter(outputPath);

        var strategy = new LocationTextExtractionStrategy();
        var totalPages = pdfDocument.GetNumberOfPages();

        for (int i = 1; i <= totalPages; i++)
        {
            var page = pdfDocument.GetPage(i);
            string text = PdfTextExtractor.GetTextFromPage(page, strategy);
            writer.WriteLine(text);

            UpdateProgress((i * 100) / totalPages);
        }
    }

    private void ConvertJpgToPng(string filePath)
    {
        string outputPath = Path.ChangeExtension(filePath, ".png");
        using var image = SDImage.FromFile(filePath);
        image.Save(outputPath, ImageFormat.Png);
        UpdateProgress(100);
    }

    private void ConvertMp3ToWav(string filePath)
    {
        string outputPath = Path.ChangeExtension(filePath, ".wav");
        using var reader = new Mp3FileReader(filePath);
        using var writer = new WaveFileWriter(outputPath, reader.WaveFormat);
        
        byte[] buffer = new byte[4096];
        int bytesRead;
        long totalBytes = reader.Length;
        long readBytes = 0;

        while ((bytesRead = reader.Read(buffer, 0, buffer.Length)) > 0)
        {
            writer.Write(buffer, 0, bytesRead);
            readBytes += bytesRead;
            UpdateProgress((int)((readBytes * 100) / totalBytes));
        }
    }

    private void UpdateProgress(int value)
    {
        if (progressBar.InvokeRequired)
        {
            progressBar.Invoke(() => progressBar.Value = value);
        }
        else
        {
            progressBar.Value = value;
        }
    }
}
