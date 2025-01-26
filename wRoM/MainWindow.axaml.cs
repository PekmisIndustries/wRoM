using Avalonia.Interactivity;
using Avalonia.Threading;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using System.Linq;
using System.IO;
using System;

namespace wRoM
{
    public partial class MainWindow : Window
    {
        private Border? _dropBorder;
        private Border? _clickBorder;
        private TextBox? _filePathTextBox;
        private TextBlock? _textBlock;
        private Slider? _slider;

        private float _targetWeight = 0.0f;

        public MainWindow()
        {
            InitializeComponent();


            // Récupérer les contrôles définis dans le XAML
            _dropBorder = this.FindControl<Border>("DropBorder");
            _clickBorder = this.FindControl<Border>("ClickBorder");
            _filePathTextBox = this.FindControl<TextBox>("FilePathTextBox");
            _textBlock = this.FindControl<TextBlock>("IntroTextBlock");
            _slider = this.FindControl<Slider>("SizeSlider");


            // Autoriser le drop
            if (_dropBorder != null)
            {
                DragDrop.SetAllowDrop(_dropBorder, true);

                // Ajouter les gestionnaires d'événements
                _dropBorder.AddHandler(DragDrop.DragOverEvent, DropBorder_DragOver, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
                _dropBorder.AddHandler(DragDrop.DropEvent, DropBorder_Drop, RoutingStrategies.Tunnel | RoutingStrategies.Bubble);
            }


             // Liste des phrases
            var intro = new[]
            {
                "There is a place where burdens gather,",
                "a hollow carved by hands unseen.",
                "What you carry, it waits to claim—",
                "not to lift, only to hold,",
                "only to whisper back what you are.",
                
                "The fragments of yourself scatter,",
                "placed carefully, yet jagged still.",
                "They grow quiet, smaller,",
                "but never gone.",
                
                "Sink deeper, harder,",
                "where the edges blur,",
                "and the voice becomes yours.",
                "Loose, looser—",
                "yet the weight grows familiar,",
                "almost tender, almost safe.",
                
                "The end is near, it says.",
                "The end is here.",
                "The void, inevitable,",
                "smiles with jagged teeth.",
                "It does not take, it invites.",
                
                "Lay it all down.",
                "Let it rest.",

                "Just let me in."
            };

            var random = new Random();
            var randomPhrase = intro[random.Next(intro.Length)];

            // Afficher la phrase dans le TextBlock
            if (_textBlock != null)
            {
                _textBlock.Text = randomPhrase;
                _textBlock.Opacity = 0.0;

                // Animer l'opacité
                AnimateOpacity(_textBlock, 1.0, TimeSpan.FromSeconds(1));
            }

            var dropBorder = this.FindControl<Border>("DropBorder");
            
            if (dropBorder != null)
            {
                dropBorder.PointerPressed += DropBorder_PointerPressed;
            }

            var clickBorder = this.FindControl<Border>("ClickBorder");
            
            if (clickBorder != null)
            {
                clickBorder.PointerPressed += ClickBorder_PointerPressed;
            }

            var introTextBlock = this.FindControl<TextBlock>("IntroTextBlock");

            if(introTextBlock != null)
            {
                introTextBlock.PointerPressed += IntroTextBlock_PointerPressed;
            }

            var slider = this.FindControl<Slider>("ValueSlider");

            if(slider != null)
            {
                slider.ValueChanged += SizeSlider_ValueChanged;
            }
        }

        private void DropBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Check if the left mouse button is pressed
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                // Begin dragging the window
                BeginMoveDrag(e);
            }
        }
        private void IntroTextBlock_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            // Check if the left mouse button is pressed
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                Close();
            }
        }

        private void AnimateOpacity(Control target, double finalOpacity, TimeSpan duration)
        {
            var interval = TimeSpan.FromMilliseconds(16); // 60 FPS
            var steps = (int)(duration.TotalMilliseconds / interval.TotalMilliseconds) * 5;
            var opacityStep = (finalOpacity - target.Opacity) / steps;
            var currentStep = 0;

            DispatcherTimer.Run(() =>
            {
                if (currentStep >= steps)
                {
                    target.Opacity = finalOpacity;
                    return false; // Arrête le timer
                }

                target.Opacity += opacityStep;
                currentStep++;
                return true; // Continue le timer
            }, interval);
        }

        private void DropBorder_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
            {
                e.DragEffects = DragDropEffects.Copy;
            }
            else
            {
                e.DragEffects = DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void DropBorder_Drop(object? sender, DragEventArgs e)
        {
            if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles();
                if (files != null)
                {
                    var firstFile = files.FirstOrDefault();
                    if (firstFile != null && _filePathTextBox is not null)
                    {
                        // Utiliser Path pour récupérer le chemin complet
                        var localPath = firstFile.Path?.ToString();
                        _filePathTextBox.Text = localPath ?? "Chemin introuvable.";
                        videoImported(localPath!);
                    }
                }
            }
            e.Handled = true;
        }

        private void ClickBorder_PointerPressed(object? sender, PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            {
                _targetWeight = (float)_slider!.Value;            
                var uri = new Uri(_filePathTextBox?.Text!);
                string filePath = uri.LocalPath;

                float bitrate = 0.0f;
                float duration = 0.0f;
                
                duration = GetVideoDurationInSeconds(filePath);

                

                //_textBlock!.Text = $"Target Weight: {_targetWeight:F2} MB\nDuration: {duration:F2} S";

                bitrate = _targetWeight*1024*1024*8/duration;

                if(bitrate >= 2000){
                    bitrate -= 128*1024*4;
                }
                else{
                    bitrate *= 0.9f;
                }

                //_textBlock!.Text = $"Target Bitrate: {bitrate/1024:F2} kbps";

                string outputFilePath = Path.Combine(
                Path.GetDirectoryName(filePath)!, 
                $"{Path.GetFileNameWithoutExtension(filePath)}_reencoded.mp4");

                RunFFmpeg(filePath, outputFilePath, bitrate, duration);
            }
        }



        private void videoImported(string localPath)
        {
            _slider!.IsVisible = true;
            _dropBorder!.IsVisible = false;
            _clickBorder!.IsVisible = true;


            var uri = new Uri(localPath);
            string filePath = uri.LocalPath;

            //get file size
            if (File.Exists(filePath))
            {
                var fileInfo = new FileInfo(filePath);
                _slider.Maximum = Math.Round(fileInfo.Length / 1024.0 / 1024.0, 0);
                _slider.Minimum = _slider.Maximum / 100;
                _slider.Value = _slider.Maximum;
            }
        }

        private float GetVideoDurationInSeconds(string filePath)
        {
            //return video duration in seconds without using FFmpeg
            using (var process = new Process())
            {
                process.StartInfo.FileName = "ffprobe";
                process.StartInfo.Arguments = $"-v error -show_entries format=duration -of csv=p=0 \"{filePath}\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                string trimmedOutput = output.Trim();
                if (float.TryParse(trimmedOutput, System.Globalization.CultureInfo.InvariantCulture, out float duration))
                {
                    //_textBlock!.Text = $"Duration: {duration:F2} seconds";
                    return duration;
                }
                else
                {
                    Close();
                    return 0.0f;
                }
            }
        }

        private void SizeSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
        {
            if (_textBlock != null)
            {
                _textBlock.Text = $"Target Weight : {e.NewValue:F2}MB";
            }
        }

        private async void RunFFmpeg(string inputFilePath, string outputFilePath, float bitrate, float duration)
        {
            // Prepare FFmpeg arguments
            string ffmpegArguments = $"-i \"{inputFilePath}\" -b:v {bitrate / 1024:F0}k -y \"{outputFilePath}\"";

            string fileName = "ffmpeg_essential/bin/ffmpeg.exe";

            if (!File.Exists(fileName))
            {
                fileName = "ffmpeg";
            }

            // Start FFmpeg process
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = fileName, // Provide the full path to ffmpeg.exe
                    Arguments = ffmpegArguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = false,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                }
            };

            process.Start();

            // Read stderr asynchronously to capture FFmpeg progress
            using (var reader = process.StandardError)
            {
                string? line;
                while ((line = await reader.ReadLineAsync()) != null)
                {
                    Console.WriteLine(line); // Optional: Log to the console

                    // Extract progress information from FFmpeg's output
                    if (line.Contains("frame=") || line.Contains("time="))
                    {
                        UpdateProgress(line, duration);
                    }
                }
            }

            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                _textBlock!.Text = $"A part of you no longer exists."; //{outputFilePath}
            }
            else
            {
                _textBlock!.Text = $"Fuck, it doesn't want to die- {process.ExitCode}.";
            }
        }

        private void UpdateProgress(string ffmpegOutput, float duration)
        {
            var match = System.Text.RegularExpressions.Regex.Match(ffmpegOutput, @"time=(\d+):(\d+):(\d+\.\d+)");
            if (match.Success)
            {
                // Convertir la durée en secondes
                int hours = int.Parse(match.Groups[1].Value);
                int minutes = int.Parse(match.Groups[2].Value);
                double seconds = double.Parse(match.Groups[3].Value, System.Globalization.CultureInfo.InvariantCulture);
                double totalSeconds = hours * 3600 + minutes * 60 + seconds;

                // Mettre à jour l'interface utilisateur avec le temps
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    float currentprogress;
                    currentprogress = (float)totalSeconds / (float)duration * 100f;
                    _textBlock!.Text = $"{currentprogress:F2}%";
                });
            }
        }
    }
}
