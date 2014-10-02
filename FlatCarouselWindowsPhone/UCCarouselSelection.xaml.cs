using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Phone.Reactive;
using Microsoft.Phone.Shell;

namespace FlatCarouselWindowsPhone
{
    public partial class UCCarouselSelection : UserControl
    {
        public UCCarouselSelection()
        {
            InitializeComponent();
            Percentage = new Subject<double>();
            Percentage.Subscribe(p =>
            {
                var opacity = 1 - (p/50);
                var scale = 1 - (p/50); 
                Ellipse.RenderTransform = new CompositeTransform{ScaleX = scale, ScaleY = scale};
                Ellipse.Opacity = opacity; 
            });
        }

        public static DependencyProperty ColorProperty =
          DependencyProperty.Register("Color", typeof(Brush), typeof(UCCarouselSelection), new PropertyMetadata(new SolidColorBrush(Colors.White)));

        public Brush Color
        {
            get
            {
                return (Brush)GetValue(ColorProperty);
            }
            set
            {
                SetValue(ColorProperty, value);
            }
        }


        public Subject<double> Percentage { get; set; }

        public void Select()
        {
            UnSelectAnimation.Stop();
            SelectAnimation.Begin(); 
        }

        public void UnSelect()
        {
            SelectAnimation.Stop();
            UnSelectAnimation.Begin();
        }
    }
}
