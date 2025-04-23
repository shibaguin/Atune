using System;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Atune.Converters;

namespace Atune.Views.Controls
{
    public partial class MultiCoverView : UserControl
    {
        public static readonly StyledProperty<IList<string>> CoverUrisProperty =
            AvaloniaProperty.Register<MultiCoverView, IList<string>>(nameof(CoverUris));

        public IList<string> CoverUris
        {
            get => GetValue(CoverUrisProperty);
            set => SetValue(CoverUrisProperty, value);
        }

        private Canvas _coverCanvas;
        private readonly CoverArtConverter _converter = new CoverArtConverter();

        public MultiCoverView()
        {
            InitializeComponent();
            _coverCanvas = this.FindControl<Canvas>("CoverCanvas");
            _coverCanvas.SizeChanged += (s, e) => RenderCovers();
        }

        // Trigger re-render when CoverUris property changes
        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);
            if (change.Property == CoverUrisProperty)
            {
                RenderCovers();
            }
        }

        private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

        private void RenderCovers()
        {
            if (_coverCanvas == null)
                return;
            _coverCanvas.Children.Clear();
            if (CoverUris == null || CoverUris.Count == 0)
                return;

            var distinctUris = CoverUris.Distinct().Take(4).ToList();
            var count = distinctUris.Count;
            var width = _coverCanvas.Bounds.Width;
            var height = _coverCanvas.Bounds.Height;
            if (width <= 0 || height <= 0)
                return;

            var culture = CultureInfo.CurrentCulture;
            switch (count)
            {
                case 1:
                    AddImage(distinctUris[0], 0, 0, width, height, culture);
                    break;
                case 2:
                    for (int i = 0; i < 2; i++)
                    {
                        var uri = distinctUris[i];
                        var bmp = _converter.Convert(uri, typeof(Bitmap), null, culture) as Bitmap;
                        if (bmp == null) continue;
                        var px = bmp.PixelSize.Width;
                        var ph = bmp.PixelSize.Height;
                        var cropPx = px / 2;
                        var xPx = i * cropPx;
                        var cropped = new CroppedBitmap(bmp, new PixelRect(xPx, 0, cropPx, ph));
                        var img = new Image
                        {
                            Source = cropped,
                            Stretch = Stretch.UniformToFill,
                            Width = width / 2,
                            Height = height
                        };
                        Canvas.SetLeft(img, i * (width / 2));
                        Canvas.SetTop(img, 0);
                        _coverCanvas.Children.Add(img);
                    }
                    break;
                case 3:
                    for (int i = 0; i < 3; i++)
                    {
                        var uri = distinctUris[i];
                        var bmp = _converter.Convert(uri, typeof(Bitmap), null, culture) as Bitmap;
                        if (bmp == null) continue;
                        var px = bmp.PixelSize.Width;
                        var ph = bmp.PixelSize.Height;
                        var cropPx = px / 3;
                        var xPx = i * cropPx;
                        var cropped = new CroppedBitmap(bmp, new PixelRect(xPx, 0, cropPx, ph));
                        var img = new Image
                        {
                            Source = cropped,
                            Stretch = Stretch.UniformToFill,
                            Width = width / 3,
                            Height = height
                        };
                        Canvas.SetLeft(img, i * (width / 3));
                        Canvas.SetTop(img, 0);
                        _coverCanvas.Children.Add(img);
                    }
                    break;
                default:
                    for (int i = 0; i < 4; i++)
                    {
                        var row = i / 2;
                        var col = i % 2;
                        var x = col * (width / 2);
                        var y = row * (height / 2);
                        AddImage(distinctUris[i], x, y, width / 2, height / 2, culture);
                    }
                    break;
            }
        }

        private void AddImage(string uri, double x, double y, double w, double h, CultureInfo culture)
        {
            var src = _converter.Convert(uri, typeof(Bitmap), null, culture) as IImage;
            if (src == null)
                return;
            var img = new Image
            {
                Source = src,
                Stretch = Stretch.Fill,
                Width = w,
                Height = h
            };
            Canvas.SetLeft(img, x);
            Canvas.SetTop(img, y);
            _coverCanvas.Children.Add(img);
        }
    }
} 