using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ImageGalleryFeature.Data;
using ImageGalleryFeature.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ImageGalleryFeature.Controllers
{
    [Route("api/[controller]")]
    public class GalleryController : Controller
    {
        private readonly ApplicationDbContext _db;
        private readonly IHostingEnvironment _env;
        public GalleryController(ApplicationDbContext db,IHostingEnvironment env)
        {
            _db = db;
            _env = env;
        }

        // Api method to get all galleries
        [HttpGet("[action]")]
        public IActionResult GetImageGallery()
        {
            var result = _db.Galleries.ToList();

            return Ok(result.Select(g => 
            new { g.GalleryId, g.Title, g.TimeCreated, g.LastUpdated,
                g.IsActive, g.IsFeatured, g.GalleryType, g.Username }));

        }

        // return featured gallery
        [HttpGet("[action]/{galleryType}")]
        public IActionResult GetFeaturedImageGallery([FromRoute]string galleryType) {

            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = from g in _db.Galleries.Where(g => g.IsFeatured == true && g.GalleryType == galleryType && g.IsActive == true)
                         join i in _db.GalleryImages on g.GalleryId equals i.GalleryId
                         select new
                         {
                             Gallery_Id = g.GalleryId,
                             Gallery_Title = g.Title,
                             Galelry_Path = g.GalleryUrl,
                             Gallery_Username = g.Username,
                             Gallery_Type = g.GalleryType,
                             Image_Id = i.ImageId,
                             Image_Path = i.ImageUrl,
                             Image_Caption = i.Caption,
                             Image_Description = i.Description,
                             Image_AltText = i.AlternateText
                         };

            return Ok(result);
        }

        // get the gallery by it's ID
        [HttpGet("[action]/{id}")]
        public IActionResult GetImageGallery([FromRoute]int id) {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var result = from g in _db.Galleries
                         join i in _db.GalleryImages.Where(t => t.GalleryId == id)
                         on g.GalleryId equals i.GalleryId
                         select new
                         {
                             Gallery_Id = g.GalleryId,
                             Gallery_Title = g.Title,
                             Galelry_Path = g.GalleryUrl,
                             Image_Id = i.ImageId,
                             Image_Path = i.ImageUrl,
                             Image_Caption = i.Caption,
                             Image_Description = i.Description,
                             Image_AltText = i.AlternateText
                         };

            if (result == null)
            {
                return NotFound();
            }

            return Ok(result);
        }

        // add new gallery in database
        [HttpPost("[action]")]
        public async Task<IActionResult> CreateNewGallery(Gallery gallery,IFormCollection formdata)
        {
            // Counter
            int i = 0;
            string GalleryTitle = formdata["GalleryTitle"];
            string GalleryType = formdata["GalleryType"];
            string Username = "arun_snc";
            DateTime LastUpdateTime = DateTime.Now;

            // First we will Create a new Gallery and get the Id of that gallery 
            int id = await CreateGalleryID(gallery);

            // Create the Gallery Path
            string GalleryPath = Path.Combine(_env.WebRootPath + $"{Path.DirectorySeparatorChar}uploads{Path.DirectorySeparatorChar}Gallery{Path.DirectorySeparatorChar}", id.ToString());

            // Path of gallery that will be stored in datatbase - No need to add full path
            string dbImageGalleryPath = Path.Combine($"{Path.DirectorySeparatorChar}uploads{Path.DirectorySeparatorChar}Gallery{Path.DirectorySeparatorChar}", id.ToString());

            // Create the Directory/Folder on Server to Store new Gallery Images
            CreateDirectory(GalleryPath);

            // Get all the files and file-details that were uploaded
            foreach (var file in formdata.Files)
            {
                if (file.Length > 0)
                {
                    // Set the extension, file name and path of the folder and file
                    var extension = Path.GetExtension(file.FileName);
                    // make the file name unique by adding date time Stamp
                    var filename = DateTime.Now.ToString("yymmssfff");
                    // Create the file path 
                    var path = Path.Combine(GalleryPath, filename) + extension;

                    // Path of Image that will be stored in datatbase - No need to add full path
                    var dbImagePath = Path.Combine(dbImageGalleryPath + $"{Path.DirectorySeparatorChar}", filename) + extension;

                    string ImageCaption = formdata["ImageCaption[]"][i];
                    string Description = formdata["ImageDescription[]"][i];
                    string AlternateText = formdata["ImageAlt[]"][i];

                    // Create the Image Model Object and assin values to its properties
                    GalleryImage Image = new GalleryImage();
                    Image.GalleryId = id;
                    Image.ImageUrl = dbImagePath;
                    Image.Caption = ImageCaption;
                    Image.Description = Description;
                    Image.AlternateText = AlternateText;

                    // Add Images detail to Images Table
                    await _db.GalleryImages.AddAsync(Image);

                    // Copy the uploaded images to Server - Uploads folder
                    // Using - Once file is copied then we will close the stream.
                    using (var stream = new FileStream(path, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    i = i + 1;

                }
            }

            gallery.LastUpdated = LastUpdateTime;
            gallery.Title = GalleryTitle;
            gallery.GalleryType = GalleryType;
            gallery.Username = Username;
            gallery.GalleryUrl = dbImageGalleryPath;
            _db.Galleries.Update(gallery);
            await _db.SaveChangesAsync();

            return new JsonResult("Successfully Added : " + GalleryTitle);
        }

        // Method to Create new Gallery ID
        private async Task<int> CreateGalleryID(Gallery gallery)
        {
            DateTime CreateTime = DateTime.Now;
            gallery.TimeCreated = CreateTime;
            _db.Galleries.Add(gallery);
            await _db.SaveChangesAsync();
            await _db.Entry(gallery).GetDatabaseValuesAsync();
            int id = gallery.GalleryId;
            return id;

        }

        // Create the gallery Path if it does not exist
        private void CreateDirectory(string gallerypath)
        {
            if (!Directory.Exists(gallerypath))
            {
                Directory.CreateDirectory(gallerypath);
            }
        }

        // Method To Delete the Gallery From Database and server
        [HttpDelete("[action]/{id}")]
        public async Task<IActionResult> DeleteGallery([FromRoute] int id)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            // Find the gallery by its id - that you want to delete
            var findGallery = await _db.Galleries.FindAsync(id);

            // If Result returned Null
            if (findGallery == null)
            {
                return NotFound();
            }
            // If Gallery With The Id Was Returned - Remove It from Database
            _db.Galleries.Remove(findGallery);

            // Now lets delete the Gallery Folder on Server
            DeleteGalleryDirectory(id);

            await _db.SaveChangesAsync();

            // Finally return success result to the client/browser
            return new JsonResult("Gallery Deleted : " + id);

        }

        private void DeleteGalleryDirectory(int id)
        {
            // First get the path of the Gallery folder
            string GalleryPath = Path.Combine(_env.WebRootPath + $"{Path.DirectorySeparatorChar}Uploads{Path.DirectorySeparatorChar}Gallery{Path.DirectorySeparatorChar}", id.ToString());

            // Store all the files with the gallery folder in this array
            string[] files = Directory.GetFiles(GalleryPath);

            // Check if the Gallery folder with that id exists
            if (Directory.Exists(GalleryPath))
            {
                // If Gallery Exists - Delete the Files inside the Gallery first
                foreach (var file in files)
                {
                    System.IO.File.SetAttributes(file, FileAttributes.Normal);
                    System.IO.File.Delete(file);
                }

                // Finally Delete the Gallery Folder
                Directory.Delete(GalleryPath);
            }

        }

        // Method to Update Gallery
        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> UpdateGallery([FromRoute]int id, IFormCollection formData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Counter for Image Files and image Caption
            int i = 0;
            int j = 0;

            // Will Hold the New Gallery Title
            string Title = formData["GalleryTitleEdit"];

            // Will Get the Details of the Gallery that needs to be Updated
            var oGallery = await _db.Galleries.FirstOrDefaultAsync(m => m.GalleryId == id);

            // Path of the Gallery that needs to be updated on Server
            string GalleryPath = Path.Combine(_env.WebRootPath + oGallery.GalleryUrl);

            // If we have received any files for update
            if (formData.Files.Count > 0)
            {
                // First we create an empty array to store old file info
                string[] filesToDeletePath = new string[formData.Files.Count];

                foreach (var file in formData.Files)
                {
                    if (file.Length > 0)
                    {
                        var extension = Path.GetExtension(file.FileName);
                        var filename = DateTime.Now.ToString("yymmssfff");
                        var path = Path.Combine(GalleryPath, filename) + extension;
                        var dbImagePath = Path.Combine(oGallery.GalleryUrl + $"{Path.DirectorySeparatorChar}", filename) + extension;
                        string ImageId = formData["imageId[]"][i];
                        // Get the info of the Image that needs to be updated
                        var updateImage = _db.GalleryImages.FirstOrDefault(o => o.ImageId == Convert.ToInt32(ImageId));

                        // First we will store path of each old file to delete in an empty array.
                        filesToDeletePath[i] = Path.Combine(_env.WebRootPath + updateImage.ImageUrl);
                        updateImage.ImageUrl = dbImagePath;

                        // Copying New Files to the Server - Gallery Folder
                        using (var stream = new FileStream(path, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Update and Save Changes to the Database
                        using (var dbContextTransaction = _db.Database.BeginTransaction())
                        {
                            try

                            {
                                _db.Entry(updateImage).State = EntityState.Modified;
                                await _db.SaveChangesAsync();

                                dbContextTransaction.Commit();
                            }
                            catch (Exception)
                            {
                                dbContextTransaction.Rollback();
                            }

                        }
                        i = i + 1;
                    }
                }

                // Delete the Old Files
                foreach (var item in filesToDeletePath)
                {
                    // If Image file Exists - Delete the File inside the Gallery folder first
                    if (System.IO.File.Exists(item))
                    {
                        System.IO.File.SetAttributes(item, FileAttributes.Normal);
                        System.IO.File.Delete(item);
                    }


                }

            }
            // Contidion Validate and Update Gallery Title and image Caption
            if (formData["imageCaption[]"].Count > 0)
            {
                oGallery.Title = Title;
                _db.Entry(oGallery).State = EntityState.Modified;

                foreach (var imgcap in formData["imageCaption[]"])
                {
                    string ImageIdCap = formData["imageId[]"][j];
                    string Caption = formData["imageCaption[]"][j];
                    string Description = formData["description[]"][j];
                    string AltText = formData["altText[]"][j];
                    var updateImageDetails = _db.GalleryImages.FirstOrDefault(o => o.ImageId == Convert.ToInt32(ImageIdCap));
                    updateImageDetails.Caption = Caption;
                    updateImageDetails.Description = Description;
                    updateImageDetails.AlternateText = AltText;
                    using (var dbContextTransaction = _db.Database.BeginTransaction())
                    {
                        try
                        {
                            _db.Entry(updateImageDetails).State = EntityState.Modified;
                            await _db.SaveChangesAsync();
                            dbContextTransaction.Commit();
                        }
                        catch (Exception)
                        {
                            dbContextTransaction.Rollback();
                        }
                    }
                    j = j + 1;
                }
            }

            return new JsonResult("Updated Successfully : ");

        }

        [HttpPut("[action]/{id}")]
        public async Task<IActionResult> UpdateGalleryById([FromRoute]int id, IFormCollection formData)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            // Will Hold the New Gallery Title
            string Title = formData["GalleryTitleEditById"];

            // Get the info of the Gallery that needs to be updated
            var updateGallery = await _db.Galleries.FirstOrDefaultAsync(o => o.GalleryId == id);

            // Will Hold the Is Active value
            if (formData["isActive"] == "on")
            {
                updateGallery.IsActive = true;
            }
            else
            {
                updateGallery.IsActive = false;
            }
            // Will Hold the Is Featured value
            if (formData["isfeatured"] == "on")
            {
                updateGallery.IsFeatured = true;
            }
            else
            {
                updateGallery.IsFeatured = false;
            }

            // update the time stamps
            updateGallery.LastUpdated = DateTime.Now;
            updateGallery.Title = Title;
            // Update and Save Changes to the Database
            using (var dbContextTransaction = _db.Database.BeginTransaction())
            {
                try

                {
                    _db.Entry(updateGallery).State = EntityState.Modified;
                    await _db.SaveChangesAsync();

                    dbContextTransaction.Commit();
                }
                catch (Exception)
                {
                    dbContextTransaction.Rollback();
                }
            }


            return new JsonResult("Updated Successfully : " + Title);


        }
    }
}