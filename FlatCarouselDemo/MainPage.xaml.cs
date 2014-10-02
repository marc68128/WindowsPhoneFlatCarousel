using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Shell;

namespace FlatCarouselDemo
{
    public partial class MainPage : PhoneApplicationPage
    {
        // Constructor
        public MainPage()
        {
            InitializeComponent();
            Carousel.ListImagesCarousel = new Dictionary<string, string>
            {
                {"/Images/1.jpg", "Image1"},
                 {"/Images/2.jpg", "Image2"},
                  {"/Images/3.jpg", "Image3"},
                   {"/Images/4.jpg", "Image4"},
                    {"/Images/5.jpg", "Image5"},
            };
        }


    }
}