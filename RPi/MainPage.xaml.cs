using System;
using System.IO;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.Devices.Gpio;
using System.Threading.Tasks;
using Windows.Media.Capture;
using System.Diagnostics;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;
using Microsoft.ProjectOxford.Emotion;
using Microsoft.ProjectOxford.Emotion.Contract;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace RPi
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {

        private const int HAPPY = 6;
        private const int NEUTRAL = 13;
        private const int SAD = 19;
        private const int ANGRY = 26;
        private const int BUTTON = 5;
        private GpioPin happyPin;
        private GpioPin neutralPin;
        private GpioPin sadPin;
        private GpioPin angryPin;
        private GpioPin pbPin;

        private string apiKey = "9c43f46d7018401aa57bb2e03b2bc6dd";

        private MediaCapture camera;
        private bool isPreviewing;

        public MainPage()
        {
            this.InitializeComponent();

            InitGPIO();
        }

        private void InitGPIO()
        {
            var gpio = GpioController.GetDefault();

            if (gpio == null)
            {
                //GpioStatus.Text = "There is no GPIO controller on this device.";
                return;
            }

            happyPin = gpio.OpenPin(HAPPY);
            neutralPin = gpio.OpenPin(NEUTRAL);
            sadPin = gpio.OpenPin(SAD);
            angryPin = gpio.OpenPin(ANGRY);
            pbPin = gpio.OpenPin(BUTTON);


            happyPin.Write(GpioPinValue.Low);
            neutralPin.Write(GpioPinValue.Low);
            sadPin.Write(GpioPinValue.Low);
            angryPin.Write(GpioPinValue.Low);

            happyPin.SetDriveMode(GpioPinDriveMode.Output);
            neutralPin.SetDriveMode(GpioPinDriveMode.Output);
            sadPin.SetDriveMode(GpioPinDriveMode.Output);
            angryPin.SetDriveMode(GpioPinDriveMode.Output);
            pbPin.SetDriveMode(GpioPinDriveMode.Input);

            pbPin.ValueChanged += PBPin_ValueChanged;
        }

        private async void PBPin_ValueChanged(GpioPin sender, GpioPinValueChangedEventArgs args)
        {
            await Dispatcher.RunAsync(Windows.UI.Core.CoreDispatcherPriority.Normal, () =>
            {
                if (args.Edge.CompareTo(GpioPinEdge.FallingEdge) == 0)
                {
                    //Pulse LED etc if new state is high.
                    {
                        takePhoto();
                    }
                }
            });
        }

        private async void button1_Click(object sender, RoutedEventArgs e)
        {
            happyPin.Write(GpioPinValue.High);
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            happyPin.Write(GpioPinValue.Low);
        }

        private async void button2_Click(object sender, RoutedEventArgs e)
        {
            neutralPin.Write(GpioPinValue.High);
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            neutralPin.Write(GpioPinValue.Low);
        }

        private async void button3_Click(object sender, RoutedEventArgs e)
        {
            sadPin.Write(GpioPinValue.High);
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            sadPin.Write(GpioPinValue.Low);
        }

        private async void button4_Click(object sender, RoutedEventArgs e)
        {
            angryPin.Write(GpioPinValue.High);
            await Task.Delay(TimeSpan.FromMilliseconds(500));
            angryPin.Write(GpioPinValue.Low);
        }

        private async void initVideo()
        {
            try
            {
                if (camera != null)
                {
                    // Cleanup MediaCapture object
                    if (isPreviewing)
                    {
                        await camera.StopPreviewAsync();
                        isPreviewing = false;
                    }

                    camera.Dispose();
                    camera = null;
                }

                camera = new MediaCapture();
                await camera.InitializeAsync();

                previewElement.Source = camera;
                await camera.StartPreviewAsync();
                isPreviewing = true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Unable to initialize camera for audio/video mode: " + ex.Message);
            }
        }

        private async void takePhoto()
        {
            try
            {
                ImageEncodingProperties imageProp = ImageEncodingProperties.CreateJpeg();
                IRandomAccessStream uwpStream = new InMemoryRandomAccessStream();
                await camera.CapturePhotoToStreamAsync(imageProp, uwpStream);
                Emotion[] emotionResult = await UploadAndDetectEmotions(
                    WindowsRuntimeStreamExtensions.AsStreamForRead(
                        uwpStream.GetInputStreamAt(0)));
                ledDriver(emotionResult[0]);
                

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        private async Task<Emotion[]> UploadAndDetectEmotions(Stream stream)
        {
            EmotionServiceClient emotionServiceClient =
                new EmotionServiceClient(apiKey);

            try
            {
                Emotion[] emotionResult;
                using (Stream imageFileStream = stream)
                {
                    emotionResult = await emotionServiceClient.RecognizeAsync(imageFileStream);
                    return emotionResult;
                }
            }
            catch (Exception exception)
            {
                Debug.WriteLine(exception.ToString());
                return null;
            }
        }

        private void button5_Click(object sender, RoutedEventArgs e)
        {
            initVideo();
        }

        private void button6_Click(object sender, RoutedEventArgs e)
        {
            takePhoto();
        }

        private async void ledDriver(Emotion emo)
        {
            if (emo.Scores.Happiness > 0.8)
            {
                happyPin.Write(GpioPinValue.High);
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                happyPin.Write(GpioPinValue.Low);
            }

            if (emo.Scores.Sadness > 0.6)
            {
                sadPin.Write(GpioPinValue.High);
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                sadPin.Write(GpioPinValue.Low);
            }

            if (emo.Scores.Neutral > 0.8)
            {
                neutralPin.Write(GpioPinValue.High);
                await Task.Delay(TimeSpan.FromMilliseconds(500));
                neutralPin.Write(GpioPinValue.Low);
            }

        }
    }
}
