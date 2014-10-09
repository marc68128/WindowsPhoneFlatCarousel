using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Shell;

namespace FlatCarouselWindowsPhone
{
    public partial class UCCarousel : UserControl
    {

        private List<Grid> _images;
        private List<UCCarouselSelection> _selectionPins;
        private int _selectedImageIndex, _nextIndex, _preIndex, _animationDone;
        private double _imageWidth, _imageHeight, _animationCompletion;
        private bool _lockAnimation, _userFlick;

        public UCCarousel()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _imageHeight = LayoutCanvas.ActualHeight;
            _imageWidth = LayoutCanvas.ActualWidth;

            LayoutCanvas.Clip = new RectangleGeometry
            {
                Rect = new Rect(new Point(0, 0), new Point(_imageWidth, _imageHeight))
            };

            ListImageChanged();
        }


        #region DependencyProperties

        static readonly DependencyProperty ListImagesCarouselProperty =
            DependencyProperty.Register("ListImagesCarousel", typeof(IEnumerable<CarouselItem>), typeof(UCCarousel),
                new PropertyMetadata((o, args) => ((UCCarousel) o).ListImageChanged()));

        static readonly DependencyProperty SelectedPinColorProperty =
            DependencyProperty.Register("SelectedPinColor", typeof (Brush), typeof (UCCarousel),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(99, 0, 0, 0))));

        static readonly DependencyProperty TitleBackgroundColorProperty =
            DependencyProperty.Register("TitleBackgroundColor", typeof(Brush), typeof(UCCarousel),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(99, 0, 0, 0))));

        static readonly DependencyProperty DisplayTitleProperty =
            DependencyProperty.Register("DisplayTitle", typeof(bool), typeof(UCCarousel),
                new PropertyMetadata(true));

        #endregion

        #region Getter & Setter

        public IEnumerable<CarouselItem> ListImagesCarousel
        {
            get { return (IEnumerable<CarouselItem>)GetValue(ListImagesCarouselProperty); }
            set { SetValue(ListImagesCarouselProperty, value); }
        }
        public Brush TitleBackgroundColor
        {
            get { return (Brush)GetValue(TitleBackgroundColorProperty); }
            set { SetValue(TitleBackgroundColorProperty, value); }
        }
        public Brush SelectedPinColor
        {
            get { return (Brush) GetValue(SelectedPinColorProperty); }
            set { SetValue(SelectedPinColorProperty, value); }
        }
        public bool DisplayTitle
        {
            get { return (bool)GetValue(DisplayTitleProperty); }
            set { SetValue(DisplayTitleProperty, value); }
        }

        #endregion

        #region PropertyChanged

        private void ListImageChanged()
        {
            if (ListImagesCarousel.Any())
                InitImageList();
        }

        #endregion

        private void InitImageList()
        {
            _images = new List<Grid>();
            _selectionPins = new List<UCCarouselSelection>();

            LayoutCanvas.Children.Clear();
            SelectionPinsStackPanel.Children.Clear();
            ProgressBar.Visibility = Visibility.Collapsed;
            foreach (var carouselItem in ListImagesCarousel)
            {
                var grid = new Grid { Height = _imageHeight, Width = _imageWidth };
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                grid.RenderTransform = new CompositeTransform();

                var image = new Image
                {
                    Source = new BitmapImage(new Uri(carouselItem.Path, UriKind.RelativeOrAbsolute)),
                    Height = _imageHeight,
                    Width = _imageWidth,
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Stretch = Stretch.Fill
                };

                image.SetValue(Grid.RowProperty, 0);
                image.SetValue(Grid.ColumnProperty, 0);
                image.SetValue(Grid.RowSpanProperty, 2);
                image.SetValue(Grid.ColumnSpanProperty, 2);

                grid.Children.Add(image);

                if (DisplayTitle)
                {
                    var titleSp = new StackPanel { Background = TitleBackgroundColor, HorizontalAlignment = HorizontalAlignment.Stretch };           
                    titleSp.SetValue(Grid.RowProperty, 1);
                    titleSp.SetValue(Grid.ColumnProperty, 0);
                    titleSp.SetValue(Grid.ColumnSpanProperty, 2);

                    var textBlock = new TextBlock
                    {
                        Text = carouselItem.Title,
                        Margin = new Thickness(10, 5, 10, 25),
                        HorizontalAlignment = HorizontalAlignment.Left,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Foreground = Foreground
                    };

                    titleSp.Children.Add(textBlock);
                    grid.Children.Add(titleSp);
                }

                _images.Add(grid);

                var selectionPin = new UCCarouselSelection { Color = SelectedPinColor };
                SelectionPinsStackPanel.Children.Add(selectionPin);
                _selectionPins.Add(selectionPin);
            }

            if (_images.Count < 3)
            {
                throw new ArgumentException("Carousel need at least 3 images to work !");
            }

            _selectedImageIndex = 0;
            _selectionPins[_selectedImageIndex].Select();
            _images[_selectedImageIndex].SetValue(Canvas.LeftProperty, 0d);
            _images[_selectedImageIndex].SetValue(Canvas.TopProperty, 0d);
            LayoutCanvas.Children.Add(_images[_selectedImageIndex]);

            _nextIndex = _selectedImageIndex + 1 <= _images.Count - 1 ? _selectedImageIndex + 1 : 0;
            _preIndex = _selectedImageIndex - 1 >= 0 ? _selectedImageIndex - 1 : _images.Count - 1;

            _images[_nextIndex].SetValue(Canvas.LeftProperty, _imageWidth);
            _images[_nextIndex].SetValue(Canvas.TopProperty, 0d);
            LayoutCanvas.Children.Add(_images[_nextIndex]);

            _images[_preIndex].SetValue(Canvas.LeftProperty, -_imageWidth);
            _images[_preIndex].SetValue(Canvas.TopProperty, 0d);
            LayoutCanvas.Children.Add(_images[_preIndex]);
        }

        #region GestureListener

        private void DragDelta(object sender, DragDeltaGestureEventArgs e)
        {
            if (!_lockAnimation)
            {
                _animationCompletion += e.HorizontalChange*100/_imageWidth;

                var scale = Math.Max(1 - 0.02*Math.Abs(_animationCompletion), 0.9);
                var opacity = Math.Max(1 - 0.06*Math.Abs(_animationCompletion), 0.7);

                _selectionPins[_selectedImageIndex].Percentage.OnNext(Math.Abs(_animationCompletion));
                if (_animationCompletion > 0)
                {
                    _selectionPins[_preIndex].Percentage.OnNext(100 - _animationCompletion);
                }
                else
                {
                    _selectionPins[_nextIndex].Percentage.OnNext(100 + _animationCompletion);
                }

                _images[_nextIndex].Opacity = opacity;
                _images[_nextIndex].RenderTransformOrigin = new Point(0.5, 0.5);
                _images[_nextIndex].RenderTransform = new CompositeTransform {ScaleX = scale, ScaleY = scale};
                _images[_preIndex].Opacity = opacity;
                _images[_preIndex].RenderTransformOrigin = new Point(0.5, 0.5);
                _images[_preIndex].RenderTransform = new CompositeTransform {ScaleX = scale, ScaleY = scale};
                _images[_selectedImageIndex].Opacity = opacity;
                _images[_selectedImageIndex].RenderTransformOrigin = new Point(0.5, 0.5);
                _images[_selectedImageIndex].RenderTransform = new CompositeTransform {ScaleX = scale, ScaleY = scale};

                _images[_nextIndex].SetValue(Canvas.LeftProperty,
                    (double) _images[_nextIndex].GetValue(Canvas.LeftProperty) + e.HorizontalChange);
                _images[_preIndex].SetValue(Canvas.LeftProperty,
                    (double) _images[_preIndex].GetValue(Canvas.LeftProperty) + e.HorizontalChange);
                _images[_selectedImageIndex].SetValue(Canvas.LeftProperty,
                    (double) _images[_selectedImageIndex].GetValue(Canvas.LeftProperty) + e.HorizontalChange);
            }


        }

        private void DragCompleted(object sender, DragCompletedGestureEventArgs e)
        {
            if (!_lockAnimation && !_userFlick)
            {
                _lockAnimation = true;
                if (_animationCompletion > 40)
                {
                    //GoToPrev
                    _animationDone = 0;
                    Animate(_images[_preIndex], 0, 2);
                    Animate(_images[_selectedImageIndex], _imageWidth, 2);
                    _selectedImageIndex = _preIndex;

                }
                else if (_animationCompletion < -40)
                {
                    //GoToNext
                    _animationDone = 0;
                    Animate(_images[_nextIndex], 0, 2);
                    Animate(_images[_selectedImageIndex], -_imageWidth, 2);
                    _selectedImageIndex = _nextIndex;
                }
                else
                {
                    //GoToCurrent
                    Animate(_images[_selectedImageIndex], 0, 3);
                    Animate(_images[_preIndex], -_imageWidth, 3);
                    Animate(_images[_nextIndex], _imageWidth, 3);
                }
                _selectionPins.ForEach(s => s.UnSelect());
                _selectionPins[_selectedImageIndex].Select();
            }
            _userFlick = false;
        }

        private void Flick(object sender, FlickGestureEventArgs e)
        {
            if (!_lockAnimation && !_userFlick)
            {
                _lockAnimation = true;
                _userFlick = true;
                if (e.HorizontalVelocity > 0)
                {
                    _animationDone = 0;
                    Animate(_images[_preIndex], 0, 3);
                    Animate(_images[_selectedImageIndex], _imageWidth, 3);
                    Animate(_images[_nextIndex], _imageWidth*2, 3);
                    _selectedImageIndex = _preIndex;
                }
                if (e.HorizontalVelocity < 0)
                {
                    _animationDone = 0;
                    Animate(_images[_nextIndex], 0, 3);
                    Animate(_images[_selectedImageIndex], -_imageWidth, 3);
                    Animate(_images[_preIndex], -2*_imageWidth, 3);
                    _selectedImageIndex = _nextIndex;
                }

                _selectionPins.ForEach(s => s.UnSelect());
                _selectionPins[_selectedImageIndex].Select();
            }
        }

        #endregion

        #region Animations

        private void Animate(Grid g, double canvasLeft, int nbOfAnim)
        {
            const int duration = 500;
            const int opacity = 1;
            const int scale = 1;

            var canvasLeftAnimation = new DoubleAnimation
            {
                To = canvasLeft,
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, duration)),
                //EasingFunction = new CircleEase { EasingMode = EasingMode.EaseOut }
            };

            var opacityAnimation = new DoubleAnimation
            {
                To = opacity,
                BeginTime = new TimeSpan(0, 0, 0, 0, 2*(duration/3)),
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, duration/3)),

            };
            var scaleXAnimation = new DoubleAnimation
            {
                To = scale,
                BeginTime = new TimeSpan(0, 0, 0, 0, 2*(duration/3)),
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, duration/3)),

            };
            var scaleYAnimation = new DoubleAnimation
            {
                To = scale,
                BeginTime = new TimeSpan(0, 0, 0, 0, 2*(duration/3)),
                Duration = new Duration(new TimeSpan(0, 0, 0, 0, duration/3)),

            };


            Storyboard.SetTarget(canvasLeftAnimation, g);
            Storyboard.SetTargetProperty(canvasLeftAnimation, new PropertyPath("(Canvas.Left)"));

            Storyboard.SetTarget(opacityAnimation, g);
            Storyboard.SetTargetProperty(opacityAnimation, new PropertyPath("(Opacity)"));

            Storyboard.SetTarget(scaleXAnimation, g);
            Storyboard.SetTargetProperty(scaleXAnimation,
                new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleX)"));

            Storyboard.SetTarget(scaleYAnimation, g);
            Storyboard.SetTargetProperty(scaleYAnimation,
                new PropertyPath("(UIElement.RenderTransform).(CompositeTransform.ScaleY)"));

            var storyboard = new Storyboard();
            storyboard.Children.Add(canvasLeftAnimation);
            storyboard.Children.Add(opacityAnimation);
            storyboard.Children.Add(scaleXAnimation);
            storyboard.Children.Add(scaleYAnimation);

            storyboard.Completed += (sender, args) => AnimationCompleted(nbOfAnim);


            storyboard.Begin();
        }

        private void AnimationCompleted(int nbOfAnim)
        {
            _animationDone++;
            if (nbOfAnim == _animationDone)
            {
                var toRemove =
                    LayoutCanvas.Children.Where(i => (double) (i as Grid).GetValue(Canvas.LeftProperty) != 0d).ToList();
                foreach (var grid in toRemove)
                {
                    LayoutCanvas.Children.Remove(grid);
                }
                _animationDone = 0;
                _lockAnimation = false;


                _animationCompletion = 0;
                _nextIndex = _selectedImageIndex + 1 <= _images.Count - 1 ? _selectedImageIndex + 1 : 0;
                _preIndex = _selectedImageIndex - 1 >= 0 ? _selectedImageIndex - 1 : _images.Count - 1;

                _images[_nextIndex].SetValue(Canvas.LeftProperty, _imageWidth);
                _images[_nextIndex].SetValue(Canvas.TopProperty, 0d);
                LayoutCanvas.Children.Add(_images[_nextIndex]);

                _images[_preIndex].SetValue(Canvas.LeftProperty, -_imageWidth);
                _images[_preIndex].SetValue(Canvas.TopProperty, 0d);
                LayoutCanvas.Children.Add(_images[_preIndex]);
            }
        }

        #endregion
    
    }

    public class CarouselItem
    {
        public CarouselItem(string imagePath)
        {
            Path = imagePath; 
        }
        public string Title { get; set; }
        public string Path { get; set; }
    }
}
