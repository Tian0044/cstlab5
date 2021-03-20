using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using lab5.Data;
using lab5.Models;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Azure;
using System.IO;

namespace lab5.Controllers
{
    public class AnswerImagesController : Controller
    {
        private readonly AnswerImageDataContext _context;
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string earthContainerName = "earthimages";
        private readonly string computerContainerName = "computerimages";

        public AnswerImagesController(AnswerImageDataContext context, BlobServiceClient blobServiceClient)
        {
            _context = context;
            _blobServiceClient = blobServiceClient;
        }
    
     /*   public AnswerImagesController(AnswerImageDataContext context)
        {
            _context = context;
        }
     */
        // GET: AnswerImages
        public async Task<IActionResult> Index()
        {
            return View(await _context.AnswerImages.ToListAsync());
        }

        // GET: AnswerImages/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var answerImage = await _context.AnswerImages
                .FirstOrDefaultAsync(m => m.AnswerImageId == id);
            if (answerImage == null)
            {
                return NotFound();
            }

            return View(answerImage);
        }

        // GET: AnswerImages/Create
        public IActionResult Upload()
        {
            return View();
        }

        // POST: AnswerImages/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Upload(Question q, IFormFile file)
        {
            BlobContainerClient containerClient;
            
            // Create the container and return a container client object
            try
            {
                if (q == Question.Computer)
                {
                    containerClient = await _blobServiceClient.CreateBlobContainerAsync(computerContainerName);
                }
                else
                {
                    containerClient = await _blobServiceClient.CreateBlobContainerAsync(earthContainerName);
                }
                // Give access to public
                containerClient.SetAccessPolicy(Azure.Storage.Blobs.Models.PublicAccessType.BlobContainer);
            }
            catch (RequestFailedException)
            {
                if (q == Question.Computer)
                {
                    containerClient = _blobServiceClient.GetBlobContainerClient(computerContainerName);
                }
                else
                {
                    containerClient = _blobServiceClient.GetBlobContainerClient(earthContainerName);
                }
            }


            try
            {
                // create the blob to hold the data
                var blockBlob = containerClient.GetBlobClient(file.FileName);
                if (await blockBlob.ExistsAsync())
                {
                    await blockBlob.DeleteAsync();
                }

                using (var memoryStream = new MemoryStream())
                {
                    // copy the file data into memory
                    await file.CopyToAsync(memoryStream);

                    // navigate back to the beginning of the memory stream
                    memoryStream.Position = 0;

                    // send the file to the cloud
                    await blockBlob.UploadAsync(memoryStream);
                    memoryStream.Close();
                }

                // add the photo to the database if it uploaded successfully
                var image = new AnswerImage();
                image.Url = blockBlob.Uri.AbsoluteUri;
                image.FileName = file.FileName;
                image.Question = q;

                _context.AnswerImages.Add(image);
                _context.SaveChanges();
            }
            catch (RequestFailedException)
            {
                View("Error");
            }

            return RedirectToAction("Index");
        }

        // GET: AnswerImages/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var answerImage = await _context.AnswerImages.FindAsync(id);
            if (answerImage == null)
            {
                return NotFound();
            }
            return View(answerImage);
        }

        // POST: AnswerImages/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("AnswerImageId,FileName,Url,Question")] AnswerImage answerImage)
        {
            if (id != answerImage.AnswerImageId)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(answerImage);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!AnswerImageExists(answerImage.AnswerImageId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(answerImage);
        }

        // GET: AnswerImages/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var answerImage = await _context.AnswerImages
                .FirstOrDefaultAsync(m => m.AnswerImageId == id);
            if (answerImage == null)
            {
                return NotFound();
            }

            return View(answerImage);
        }

        // POST: AnswerImages/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id, Question q)
        {
            var answerImage = await _context.AnswerImages.FindAsync(id);
            BlobContainerClient containerClient;
            // Get the container and return a container client object
            try
            {
                if (q == Question.Computer)
                {
                    containerClient = _blobServiceClient.GetBlobContainerClient(computerContainerName);
                }
                else
                {
                    containerClient = _blobServiceClient.GetBlobContainerClient(earthContainerName);
                }
            }
            catch (RequestFailedException)
            {
                return View("Error");
            }

            try
            {
                // Get the blob that holds the data
                var blockBlob = containerClient.GetBlobClient(answerImage.FileName);
                if (await blockBlob.ExistsAsync())
                {
                    await blockBlob.DeleteAsync();
                }

                _context.AnswerImages.Remove(answerImage);
                await _context.SaveChangesAsync();

            }
            catch (RequestFailedException)
            {
                return View("Error");
            }

            return RedirectToAction("Index");
        }

        private bool AnswerImageExists(int id)
        {
            return _context.AnswerImages.Any(e => e.AnswerImageId == id);
        }
    }
}
