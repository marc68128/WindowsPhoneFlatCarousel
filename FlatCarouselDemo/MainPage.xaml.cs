using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using FlatCarouselWindowsPhone;
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
            var carouselItemList = new List<CarouselItem>();
            for (int i = 1; i < 6; i++)
            {
                carouselItemList.Add(new CarouselItem
                {
                    Path = "/Images/" + i + ".jpg", 
                    Title = "Image " + i
                });
            }
            Carousel.ListImagesCarousel = carouselItemList; 
        }


    }
}