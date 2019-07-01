using ImageGalleryFeature.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ImageGalleryFeature.Data
{
    public class ApplicationDbContext:DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base(options)
        {

        }

        // All the required DbSets entities

        public DbSet<Gallery> Galleries { get; set; }

        public DbSet<GalleryImage> GalleryImages { get; set; }
    }
}
