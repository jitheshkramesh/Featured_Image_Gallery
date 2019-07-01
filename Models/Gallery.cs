using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGalleryFeature.Models
{
    public class Gallery
    {
        [Key]
        public int GalleryId { get; set; }

        // The Gallery Url to Access the Gallery by its Gallery ID 

        public string GalleryUrl { get; set; }

        // Our Gallery Will Have a title

        public string Title { get; set; }

        // Time Created
        public DateTime TimeCreated { get; set; }

        public DateTime LastUpdated { get; set; }

        public string UserId { get; set; }

        public string Username { get; set; }

        public bool IsFeatured { get; set; }

        public bool IsActive { get; set; }

        public string GalleryType { get; set; }

        // using EF Core feature we will create relation between Gallery and Gallery Images
        public ICollection<GalleryImage> GalleryImages { get; set; }

    }
}