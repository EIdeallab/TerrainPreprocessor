using System;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Win32;

namespace TerrainPreprocessor
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : Window
    {
        private TiffIO IO { get; set; }
        private DemController Controller { get; set; }
        private string FilePath { get; set; }
        private int Resolution { get; } = 2049;
        private short[,] resultArea { get; set; }

        private Point scrollMousePoint = new Point();
        private double hOff = 1;
        private double vOff = 1;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void SetPath_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    FilePath = dialog.SelectedPath;
                    
                    Longitude.IsEnabled = true;
                    Latitude.IsEnabled = true;
                    Process.IsEnabled = true;
                    Radius.IsEnabled = true;

                    // Tiff IO 생성
                    IO = new TiffIO();
                    // Height 변환기 생성
                    Controller = new DemController();

                    LogWindow.Text = "Path : " + dialog.SelectedPath + "\nPlease Input area. ex) lon = 126.2, lat = 37.3, rad = 10000 \n";
                }
            }
        }

        private void Process_Click(object sender, RoutedEventArgs e)
        {
            bool bLon = float.TryParse(Longitude.Text, out float lon);
            bool bLat = float.TryParse(Latitude.Text, out float lat);
            bool bRad = int.TryParse(Radius.Text, out int rad);

            if (bLon && bLat && bRad)
            {
                AreaRange range = Controller.GetAreaRange(lon, lat, rad);
                string format = comboBox.SelectionBoxItem as string;
                string minminPath = FilePath + @"\N" + ((int)range.minPt.Y).ToString("000") + "E" + ((int)range.minPt.X).ToString("000") + "_AVE_" + format + ".tif";
                string minmaxPath = FilePath + @"\N" + ((int)range.maxPt.Y).ToString("000") + "E" + ((int)range.minPt.X).ToString("000") + "_AVE_" + format + ".tif";
                string maxminPath = FilePath + @"\N" + ((int)range.minPt.Y).ToString("000") + "E" + ((int)range.maxPt.X).ToString("000") + "_AVE_" + format + ".tif";
                string maxmaxPath = FilePath + @"\N" + ((int)range.maxPt.Y).ToString("000") + "E" + ((int)range.maxPt.X).ToString("000") + "_AVE_" + format + ".tif";

                string log = "";

                // 좌하단 위경도 tiff 로드
                short[,] minmin = LoadTiff(minminPath, ref log);

                // 좌상단 위경도 tiff 로드
                short[,] minmax = LoadTiff(minmaxPath, ref log);

                // 우하단 위경도 tiff 로드
                short[,] maxmin = LoadTiff(maxminPath, ref log);

                // 우상단 위경도 tiff 로드
                short[,] maxmax = LoadTiff(maxmaxPath, ref log);

                // 로그 출력
                LogWindow.Text = log;

                // 클립, 리사이징
                Controller.FileHeight = (IO.ImageHeight != 0)? IO.ImageHeight : Resolution;
                Controller.FileWidth = (IO.ImageWidth != 0)? IO.ImageWidth : Resolution;
                short[,] clipArea = Controller.ClipSpecificArea(minmin, minmax, maxmin, maxmax, range);
                short[,] refineArea = Controller.ResizeHeightMap(clipArea, Resolution);

                // 사용자 정의 변환
                resultArea = Controller.ConvCustom(refineArea);

                RenderTiff(resultArea);

                Save.IsEnabled = true;
            }
            else
            {
                // 입력 포맷 불일치
                LogWindow.Text = "Input format is invalid. Please re - input area. ex) lon = 126.2, lat = 37.3, rad = 100000 \n";
            }
        }

        private void RenderTiff(short[,] pixels)
        {
            int width = pixels.GetLength(1);
            int height = pixels.GetLength(0);

            Image.Width = width + 1;
            Image.Height = height + 1;

            WriteableBitmap writeableBitmap = new WriteableBitmap(width + 1, height + 1, 300d, 300d, PixelFormats.Gray16, null);

            try
            {
                // Reserve the back buffer for updates.
                writeableBitmap.Lock();

                unsafe
                {
                    for(int y = 0; y < height; y++)
                    {
                        // Get a pointer to the back buffer.
                        int pBackBuffer = (int)writeableBitmap.BackBuffer;
                        pBackBuffer += writeableBitmap.BackBufferStride * y;

                        for (int x = 0; x < width; x++)
                        {
                            // Assign the color data to the pixel.
                            *((int*)pBackBuffer) = pixels[y, x] * (65536/2000);
                            pBackBuffer += sizeof(short);
                        }
                    }
                }

                // Specify the area of the bitmap that changed.
                writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, width, height));
            }
            finally
            {
                // Release the back buffer and make it available for display.
                writeableBitmap.Unlock();
            }
            
            Image.Source = writeableBitmap; // This should be done only once
        }
        
        private short[,] LoadTiff(string path, ref string log)
        {
            // tiff 파일 로드
            Exception errorMsg;
            short[,] rt;
            if (IO.LoadTiff(path, out errorMsg))
            {
                rt = IO.GetHeightsShort(0);
                log += path + " File Loaded Successfully\n";
            }
            else
            {
                rt = new short[Resolution, Resolution];
                log += path + " File Load Failed\n" + errorMsg.ToString();
            }
            return rt;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            IO.SetHeightsShort(resultArea, 0);
            IO.ImageHeight = (ushort)Resolution;
            IO.ImageWidth = (ushort)Resolution;

            if (IO.SaveTiff(FilePath + @"\MOD_AVE_DSM.tif"))
            {
                LogWindow.Text = " Tiff file save has been completed.\n";
            }
            else
            {
                LogWindow.Text = "Tiff file save has been Failed.\n";
            }
        }

        private void TextBox_Focus(object sender, RoutedEventArgs e)
        {
            ((System.Windows.Controls.TextBox)sender).Text = "";
        }

        private void TextBox_NotFocus(object sender, RoutedEventArgs e)
        {
            if(((System.Windows.Controls.TextBox)sender).Text == "")
                ((System.Windows.Controls.TextBox)sender).Text = ((System.Windows.Controls.TextBox)sender).Name;
        }
        
        private void scrollViewer_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            scrollMousePoint = e.GetPosition(scrollViewer);
            hOff = scrollViewer.HorizontalOffset;
            vOff = scrollViewer.VerticalOffset;
            scrollViewer.CaptureMouse();
        }

        private void scrollViewer_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (scrollViewer.IsMouseCaptured)
            {
                scrollViewer.ScrollToHorizontalOffset(hOff + (scrollMousePoint.X - e.GetPosition(scrollViewer).X));
                scrollViewer.ScrollToVerticalOffset(vOff + (scrollMousePoint.Y - e.GetPosition(scrollViewer).Y));
            }
        }

        private void scrollViewer_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            scrollViewer.ReleaseMouseCapture();
        }

        private void scrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            double zoom = e.Delta > 0 ? 0.02 : -0.02;
            imageScale.ScaleX += zoom;
            imageScale.ScaleY += zoom;
            e.Handled = true;
        }
    }
}
