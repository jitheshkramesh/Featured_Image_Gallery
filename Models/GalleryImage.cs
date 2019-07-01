using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGalleryFeature.Models
{
    public class GalleryImage
    {    
        [Key]
        public int ImageId { get; set; }

        // Url of the image- where the image is actually stored
        public string ImageUrl { get; set; }

        // Image Caption 
        public string Caption { get; set; }

        // Every image belongs to a gallery and Every Gallery Has an Gallery ID 
        public int GalleryId { get; set; }

        // Image Description 
        public string Description { get; set; }

        // Image Alternate Text
        public string AlternateText { get; set; }

        public Gallery Gallery { get; set; }
    }
}
