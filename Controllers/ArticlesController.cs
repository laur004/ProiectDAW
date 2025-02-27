﻿using ProiectDAW.Data;
using ProiectDAW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Authorization;

namespace ProiectDAW.Controllers
{
    public class ArticlesController : Controller
    {   

        //PAS 10

        private readonly IWebHostEnvironment _env;
        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public ArticlesController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IWebHostEnvironment env
        )
        {
            _env = env;
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }



        // Se afiseaza lista tuturor articolelor impreuna cu categoria 
        // din care fac parte
        // HttpGet implicit cu sortare
        //[Authorize(Roles = "Admin,User")]
        public IActionResult Index(string sortOrder= "date_desc")
        {
            var articles = db.Articles.Include("Chapter")
                                        .Include("Chapter.Grade")
                                        .Include("User");
                                        
            
            //ViewBag.GradeName=
            // Pregătim parametrii de sortare pentru View
            ViewData["DateSortParam"] = sortOrder == "date_asc" ? "date_desc" : "date_asc";
            ViewData["TitleSortParam"] = sortOrder == "title_asc" ? "title_desc" : "title_asc";

            //var articles = db.Articles.Include("Chapter");

            // Logica de sortare
            switch (sortOrder)
            {
                case "date_asc":
                    articles = articles.OrderBy(a => a.Date);
                    break;
                case "date_desc":
                    articles = articles.OrderByDescending(a => a.Date);
                    break;
                case "title_asc":
                    articles = articles.OrderBy(a => a.Title);
                    break;
                case "title_desc":
                    articles = articles.OrderByDescending(a => a.Title);
                    break;
                default:
                    articles = articles.OrderByDescending(a => a.Date); // Sortare implicită
                    break;
            }

            // ViewBag.OriceDenumireSugestiva
            ViewBag.Articles = articles;


            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }


            // MOTOR DE CAUTARE

            var search = "";

            if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            {
                search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 

                // Cautare in articol (Title si Content)

                List<int> articleIds = db.Articles.Where
                                        (
                                         at => at.Title.Contains(search)
                                         || at.Content.Contains(search)
                                        ).Select(a => a.Id).ToList();

                // Cautare in comentarii (Content)
                List<int> articleIdsOfCommentsWithSearchString = db.Comments
                                        .Where
                                        (
                                         c => c.Content.Contains(search)
                                        ).Select(c => (int)c.ArticleId).ToList();

                // Se formeaza o singura lista formata din toate id-urile selectate anterior
                List<int> mergedIds = articleIds.Union(articleIdsOfCommentsWithSearchString).ToList();


                // Lista articolelor care contin cuvantul cautat
                // fie in articol -> Title si Content
                // fie in comentarii -> Content
                articles = db.Articles.Where(article => mergedIds.Contains(article.Id))
                                      .Include("Chapter")
                                      .Include("User")
                                      .OrderByDescending(a => a.Date);

            }

            ViewBag.SearchString = search;

            // AFISARE PAGINATA

            // Alegem sa afisam 3 articole pe pagina
            int _perPage = 3;

            // Fiind un numar variabil de articole, verificam de fiecare data utilizand 
            // metoda Count()

            int totalItems = articles.Count();

            // Se preia pagina curenta din View-ul asociat
            // Numarul paginii este valoarea parametrului page din ruta
            // /Articles/Index?page=valoare

            var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);

            // Pentru prima pagina offsetul o sa fie zero
            // Pentru pagina 2 o sa fie 3 
            // Asadar offsetul este egal cu numarul de articole care au fost deja afisate pe paginile anterioare
            var offset = 0;

            // Se calculeaza offsetul in functie de numarul paginii la care suntem
            if (!currentPage.Equals(0))
            {
                offset = (currentPage - 1) * _perPage;
            }

            // Se preiau articolele corespunzatoare pentru fiecare pagina la care ne aflam 
            // in functie de offset
            var paginatedArticles = articles.Skip(offset).Take(_perPage);


            // Preluam numarul ultimei pagini
            ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            // Trimitem articolele cu ajutorul unui ViewBag catre View-ul corespunzator
            ViewBag.Articles = paginatedArticles;

            // DACA AVEM AFISAREA PAGINATA IMPREUNA CU SEARCH

            if (search != "")
            {
                ViewBag.PaginationBaseUrl = "/Articles/Index/?search=" + search + "&page";
            }
            else
            {
                ViewBag.PaginationBaseUrl = "/Articles/Index/?page";
            }

            return View(articles.ToList());
        }
        //private readonly ApplicationDbContext db;
        //private readonly IWebHostEnvironment _env;
        //public ArticlesController(ApplicationDbContext context, IWebHostEnvironment env)
        //{
        //    db = context;
        //    _env = env;
        //}

        // Se afiseaza lista tuturor articolelor impreuna cu categoria 
        // din care fac parte
        // HttpGet implicit

        //[Authorize(Roles = "Admin,User")]
        /*public IActionResult Index()
        {
            var articles = db.Articles.Include("Chapter")
                                        .Include("User");

            // ViewBag.OriceDenumireSugestiva
            ViewBag.Articles = articles;

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
                ViewBag.Alert = TempData["messageType"];
            }

            return View();
        }*/

        // Se afiseaza un singur articol in functie de id-ul sau 
        // impreuna cu categoria din care face parte
        // In plus sunt preluate si toate comentariile asociate unui articol
        // HttpGet implicit
        public IActionResult Show(int id)
        {
            Article article = db.Articles.Include("Chapter")
                                         .Include("Comments")
                                         .Include("User")
                                         .Include("Comments.User")
                              .Where(art => art.Id == id)
                              .First();

            SetAccessRights();

            return View(article);
        }




        // Adaugarea unui comentariu asociat unui articol in baza de date
        // Toate rolurile pot adauga comentarii in baza de date
        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public IActionResult Show([FromForm] Comment comment)
        {
            comment.Date = DateTime.Now;

            // preluam Id-ul utilizatorului care posteaza comentariul
            comment.UserId = _userManager.GetUserId(User);

            try
            {
                db.Comments.Add(comment);
                db.SaveChanges();
                return Redirect("/Articles/Show/" + comment.ArticleId);
            }
            catch (Exception)
            {
                Article art = db.Articles.Include("Chapter")
                                         .Include("User")
                                         .Include("Comments")
                                         .Include("Comments.User")
                               .Where(art => art.Id == comment.ArticleId)
                               .First();

                //return Redirect("/Articles/Show/" + comm.ArticleId);

                SetAccessRights();

                return View(art);
            }
        }



        // Se afiseaza formularul in care se vor completa datele unui articol
        // impreuna cu selectarea categoriei din care face parte
        // HttpGet implicit

        [Authorize(Roles ="User,Editor,Admin")]
        public IActionResult New()
        {
          
            Article article = new Article();

            article.Chap = GetAllChapters();

            return View(article);
        }

        // POST: Procesează datele trimise de utilizator

        //[HttpPost]
        //[Authorize(Roles = "User,Admin")]
        //public async Task<IActionResult> New(Article article, IFormFile Image)
        //{
        //    article.Date = DateTime.Now;

        //    //preluam id-ul user-ului
        //    article.UserId = _userManager.GetUserId(User);


        //    if (Image != null && Image.Length > 0)
        //    {
        //        // Verificăm extensia
        //        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".mp4", ".mov" };
        //        var fileExtension = Path.GetExtension(Image.FileName).ToLower();
        //        if (!allowedExtensions.Contains(fileExtension))
        //        {
        //            ModelState.AddModelError("ArticleImage", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif) sau un video (mp4,  mov).");

        //            article.Chap = GetAllChapters();

        //            return View(article);
        //        }

        //        // Cale stocare
        //        var storagePath = Path.Combine(_env.WebRootPath, "images", Image.FileName);
        //        var databaseFileName = "/images/" + Image.FileName;

        //        // Salvare fișier
        //        using (var fileStream = new FileStream(storagePath, FileMode.Create))
        //        {
        //            await Image.CopyToAsync(fileStream);
        //        }

        //        ModelState.Remove(nameof(article.Image));


        //        article.Image = databaseFileName;

        //    }

        //    if(TryValidateModel(article))
        //    {
        //        // Adăugare articol
        //        db.Articles.Add(article);
        //        await db.SaveChangesAsync();

        //        TempData["message"] = "Articolul a fost adaugat";
        //        TempData["messageType"] = "alert-success";

        //        // Redirecționare după succes
        //        return RedirectToAction("Index", "Articles");
        //    }

        //    article.Chap = GetAllChapters();
        //    return View(article);
        //}












        [HttpPost]
        [Authorize(Roles = "User,Editor,Admin")]
        public async Task<IActionResult> New(Article article, IFormFile? Image, IFormFile? PdfPath)
        {
            if (!ModelState.IsValid)
            {
                article.Chap = GetAllChapters();
                return View(article);
            }

            try
            {
                article.Date = DateTime.Now;
                article.UserId = _userManager.GetUserId(User);

                // Procesare imagine
                if (Image != null && Image.Length > 0)
                {
                    var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                    var imageExtension = Path.GetExtension(Image.FileName).ToLower();

                    if (!allowedImageExtensions.Contains(imageExtension))
                    {
                        ModelState.AddModelError("Image", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif).");
                        article.Chap = GetAllChapters();
                        return View(article);
                    }

                    var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(Image.FileName);
                    var imageStoragePath = Path.Combine(_env.WebRootPath, "images", uniqueFileName);
                    var imageDatabasePath = "/images/" + uniqueFileName;

                    using (var fileStream = new FileStream(imageStoragePath, FileMode.Create))
                    {
                        await Image.CopyToAsync(fileStream);
                    }

                    article.Image = imageDatabasePath;
                }

                // Procesare PDF
                if (PdfPath != null && PdfPath.Length > 0)
                {

                    var allowedPdfExtensions = new[] { ".pdf" };
                    var pdfExtension = Path.GetExtension(PdfPath.FileName).ToLower();

                    if (!allowedPdfExtensions.Contains(pdfExtension))
                    {
                        ModelState.AddModelError("PdfPath", "Fișierul trebuie să fie un document PDF.");
                        article.Chap = GetAllChapters();
                        return View(article);
                    }

                    var uniquePdfName = Guid.NewGuid().ToString() + Path.GetExtension(PdfPath.FileName);
                    var pdfStoragePath = Path.Combine(_env.WebRootPath, "pdfs", uniquePdfName);
                    var pdfDatabasePath = "/pdfs/" + uniquePdfName;

                    using (var fileStream = new FileStream(pdfStoragePath, FileMode.Create))
                    {
                        await PdfPath.CopyToAsync(fileStream);
                    }

                    article.PdfPath = pdfDatabasePath;
                }

                db.Articles.Add(article);
                await db.SaveChangesAsync();

                TempData["message"] = "Articolul a fost adăugat cu succes!";
                TempData["messageType"] = "alert-success";

                return RedirectToAction("Index", "Articles");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", "A apărut o eroare la salvarea articolului.");
                article.Chap = GetAllChapters();
                return View(article);
            }
        }












        //[HttpPost]
        //[Authorize(Roles = "Admin,User")]
        //public async Task<IActionResult> Edit(int id, Article requestArticle, IFormFile? Image, IFormFile? PdfPath)
        //{
        //    Article article = db.Articles.Find(id);
        //    if (article == null)
        //    {
        //        TempData["message"] = "Articolul nu a fost găsit!";
        //        TempData["messageType"] = "alert-danger";
        //        return RedirectToAction("Index");
        //    }

        //    try
        //    {
        //        if ((article.UserId == _userManager.GetUserId(User)) || User.IsInRole("Admin"))
        //        {
        //            // Actualizăm câmpurile articolului
        //            article.Title = requestArticle.Title;
        //            article.Content = requestArticle.Content;
        //            article.Date = DateTime.Now;
        //            article.ChapterId = requestArticle.ChapterId;

        //            // Procesare imagine (dacă s-a ales o nouă imagine)
        //            if (Image != null && Image.Length > 0)
        //            {
        //                var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        //                var imageExtension = Path.GetExtension(Image.FileName).ToLower();

        //                if (!allowedImageExtensions.Contains(imageExtension))
        //                {
        //                    ModelState.AddModelError("Image", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif).");
        //                    requestArticle.Chap = GetAllChapters();
        //                    return View(requestArticle);
        //                }

        //                var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(Image.FileName);  // Nume unic pentru fișier
        //                var imageStoragePath = Path.Combine(_env.WebRootPath, "images", uniqueFileName);
        //                var imageDatabasePath = "/images/" + uniqueFileName;

        //                using (var fileStream = new FileStream(imageStoragePath, FileMode.Create))
        //                {
        //                    await Image.CopyToAsync(fileStream);
        //                }

        //                article.Image = imageDatabasePath;  // Actualizăm referința la imagine
        //            }

        //            // Procesare PDF (dacă s-a ales un nou fișier PDF)
        //            if (PdfPath != null && PdfPath.Length > 0)
        //            {
        //                var allowedPdfExtensions = new[] { ".pdf" };
        //                var pdfExtension = Path.GetExtension(PdfPath.FileName).ToLower();

        //                if (!allowedPdfExtensions.Contains(pdfExtension))
        //                {
        //                    ModelState.AddModelError("PdfPath", "Fișierul trebuie să fie un document PDF.");
        //                    requestArticle.Chap = GetAllChapters();
        //                    return View(requestArticle);
        //                }

        //                var uniquePdfName = Guid.NewGuid().ToString() + Path.GetExtension(PdfPath.FileName);  // Nume unic pentru PDF
        //                var pdfStoragePath = Path.Combine(_env.WebRootPath, "pdfs", uniquePdfName);
        //                var pdfDatabasePath = "/pdfs/" + uniquePdfName;

        //                using (var fileStream = new FileStream(pdfStoragePath, FileMode.Create))
        //                {
        //                    await PdfPath.CopyToAsync(fileStream);
        //                }

        //                article.PdfPath = pdfDatabasePath;  // Actualizăm referința la PDF
        //            }

        //            // Salvăm modificările în baza de date
        //            db.SaveChanges();

        //            TempData["message"] = "Articolul a fost modificat cu succes!";
        //            TempData["messageType"] = "alert-success";

        //            return RedirectToAction("Index");
        //        }
        //        else
        //        {
        //            TempData["message"] = "Nu aveți dreptul să editați acest articol!";
        //            TempData["messageType"] = "alert-danger";
        //            return RedirectToAction("Index");
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        // Log the error
        //        TempData["message"] = $"A apărut o eroare la salvarea articolului! Detalii: {ex.Message}";
        //        TempData["messageType"] = "alert-danger";
        //        return View(requestArticle);
        //    }
        //}
        [HttpPost]
        [Authorize(Roles = "User,Admin,Editor")]
        public async Task<IActionResult> Edit(int id, Article requestArticle, IFormFile? Image, IFormFile? PdfPath, bool DeleteImage, bool DeletePdf)
        {

            Article article = db.Articles.Find(id);
            if (article == null)
            {
                TempData["message"] = "Articolul nu a fost găsit!";
                TempData["messageType"] = "alert-danger";
                return RedirectToAction("Index");
            }

            try
            {
                // Verificăm rolul și drepturile
                bool isAdmin = User.IsInRole("Admin");
                bool isEditor = User.IsInRole("Editor");
                bool isOwner = article.UserId == _userManager.GetUserId(User);

                if (isAdmin || isEditor || isOwner)
                {
                    // Dacă este doar editor, actualizăm doar capitolul
                    if (isEditor && !isAdmin && !isOwner)
                    {
                        article.ChapterId = requestArticle.ChapterId;
                    }
                    // Dacă este admin sau proprietar, poate actualiza tot
                    else if (isAdmin || isOwner)
                    {
                        article.Title = requestArticle.Title;
                        article.Content = requestArticle.Content;
                        article.Date = DateTime.Now;
                        article.ChapterId = requestArticle.ChapterId;

                        // Ștergere imagine
                        if (DeleteImage && !string.IsNullOrEmpty(article.Image))
                        {
                            var imagePath = Path.Combine(_env.WebRootPath, article.Image.TrimStart('/'));
                            if (System.IO.File.Exists(imagePath))
                            {
                                System.IO.File.Delete(imagePath);
                            }
                            article.Image = null;
                        }

                        // Ștergere PDF
                        if (DeletePdf && !string.IsNullOrEmpty(article.PdfPath))
                        {
                            var pdfPath = Path.Combine(_env.WebRootPath, article.PdfPath.TrimStart('/'));
                            if (System.IO.File.Exists(pdfPath))
                            {
                                System.IO.File.Delete(pdfPath);
                            }
                            article.PdfPath = null;
                        }

                        // Procesare imagine nouă
                        if (Image != null && Image.Length > 0)
                        {
                            var allowedImageExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                            var imageExtension = Path.GetExtension(Image.FileName).ToLower();

                            if (!allowedImageExtensions.Contains(imageExtension))
                            {
                                ModelState.AddModelError("Image", "Fișierul trebuie să fie o imagine (jpg, jpeg, png, gif).");
                                return View(requestArticle);
                            }

                            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(Image.FileName);
                            var imageStoragePath = Path.Combine(_env.WebRootPath, "images", uniqueFileName);
                            var imageDatabasePath = "/images/" + uniqueFileName;

                            using (var fileStream = new FileStream(imageStoragePath, FileMode.Create))
                            {
                                await Image.CopyToAsync(fileStream);
                            }

                            article.Image = imageDatabasePath;
                        }

                        // Procesare PDF nou
                        if (PdfPath != null && PdfPath.Length > 0)
                        {
                            var allowedPdfExtensions = new[] { ".pdf" };
                            var pdfExtension = Path.GetExtension(PdfPath.FileName).ToLower();

                            if (!allowedPdfExtensions.Contains(pdfExtension))
                            {
                                ModelState.AddModelError("PdfPath", "Fișierul trebuie să fie un document PDF.");
                                return View(requestArticle);
                            }

                            var uniquePdfName = Guid.NewGuid().ToString() + Path.GetExtension(PdfPath.FileName);
                            var pdfStoragePath = Path.Combine(_env.WebRootPath, "pdfs", uniquePdfName);
                            var pdfDatabasePath = "/pdfs/" + uniquePdfName;

                            using (var fileStream = new FileStream(pdfStoragePath, FileMode.Create))
                            {
                                await PdfPath.CopyToAsync(fileStream);
                            }

                            article.PdfPath = pdfDatabasePath;
                        }
                    }

                    // Salvăm modificările
                    db.SaveChanges();

                    TempData["message"] = "Articolul a fost modificat cu succes!";
                    TempData["messageType"] = "alert-success";

                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["message"] = "Nu aveți dreptul să editați acest articol!";
                    TempData["messageType"] = "alert-danger";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["message"] = $"A apărut o eroare la salvarea articolului! Detalii: {ex.Message}";
                TempData["messageType"] = "alert-danger";
                return View(requestArticle);
            }
        }
















        // Se editeaza un articol existent in baza de date impreuna cu categoria din care face parte
        // Categoria se selecteaza dintr-un dropdown
        // HttpGet implicit
        // Se afiseaza formularul impreuna cu datele aferente articolului din baza de date

        [Authorize(Roles = "User,Admin,Editor")]
        public IActionResult Edit(int id)
        {
            SetAccessRights();

            Article article = db.Articles.Include("Chapter")
                                         .Where(art => art.Id == id)
                                         .First();

            article.Chap = GetAllChapters();

            if ((article.UserId == _userManager.GetUserId(User)) || User.IsInRole("Admin") || User.IsInRole("Editor"))
            {
                return View(article);
            }
            else
            {

                TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
                //return RedirectToAction("Show", new { id });
            }
        }



















        // Se adauga articolul modificat in baza de date
        //[HttpPost]
        //[Authorize(Roles = "Admin,User")]
        //public IActionResult Edit(int id, Article requestArticle)
        //{
        //    Article article = db.Articles.Find(id);

        //    try
        //    {
        //        if ((article.UserId == _userManager.GetUserId(User)) || User.IsInRole("Admin"))
        //        {

        //            article.Title = requestArticle.Title;
        //            article.Content = requestArticle.Content;
        //            article.Date = DateTime.Now;
        //            article.ChapterId = requestArticle.ChapterId;
        //            db.SaveChanges();
        //            TempData["message"] = "Articolul a fost modificat";
        //            TempData["messageType"] = "alert-success";

        //            return RedirectToAction("Index");
        //            //return RedirectToAction("Show", new { id });

        //        }
        //        else
        //        {
        //            TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
        //            TempData["messageType"] = "alert-danger";

        //            return RedirectToAction("Index");
        //            //return RedirectToAction("Show", new { id });
        //        }

        //    }
        //    catch (Exception e)
        //    {
        //        requestArticle.Chap = GetAllChapters();
        //        return View(requestArticle);
        //    }
        //}










        [HttpPost]
        [Authorize(Roles = "Admin,Editor,User")]
        public ActionResult Delete(int id)
        {
            // Căutăm articolul în baza de date și includem comentariile asociate
            Article article = db.Articles.Include("Comments")
                                         .Where(art => art.Id == id)
                                         .FirstOrDefault();

            // Verificăm dacă articolul există și dacă utilizatorul are dreptul să îl șteargă
            if (article != null && ((article.UserId == _userManager.GetUserId(User)) || User.IsInRole("Admin") ))
            {
                try
                {
                    // Ștergem imaginea dacă există
                    if (!string.IsNullOrEmpty(article.Image))
                    {
                        var imagePath = Path.Combine(_env.WebRootPath, article.Image.TrimStart('/'));
                        if (System.IO.File.Exists(imagePath))
                        {
                            System.IO.File.Delete(imagePath);
                        }
                    }

                    // Ștergem fișierul PDF dacă există
                    if (!string.IsNullOrEmpty(article.PdfPath))
                    {
                        var pdfPath = Path.Combine(_env.WebRootPath, article.PdfPath.TrimStart('/'));
                        if (System.IO.File.Exists(pdfPath))
                        {
                            System.IO.File.Delete(pdfPath);
                        }
                    }

                    // Ștergem articolul din baza de date
                    db.Articles.Remove(article);
                    db.SaveChanges();

                    // Afișăm un mesaj de succes
                    TempData["message"] = "Articolul a fost șters cu succes!";
                    TempData["messageType"] = "alert-success";

                    return RedirectToAction("Index");
                }
                catch (Exception ex)
                {
                    // În caz de eroare, afișăm un mesaj de eroare
                    TempData["message"] = "A apărut o eroare la ștergerea articolului.";
                    TempData["messageType"] = "alert-danger";

                    return RedirectToAction("Index");
                }
            }
            else
            {
                // Dacă articolul nu există sau utilizatorul nu are dreptul de a-l șterge
                TempData["message"] = "Nu aveți dreptul să ștergeți acest articol!";
                TempData["messageType"] = "alert-danger";

                return RedirectToAction("Index");
            }
        }








        // Se sterge un articol din baza de date 
        //[HttpPost]
        //[Authorize(Roles = "Admin,User")]
        //public ActionResult Delete(int id)
        //{
        //    Article article = db.Articles.Include("Comments")
        //                                 .Where(art => art.Id == id)
        //                                 .First();

        //    if ((article.UserId == _userManager.GetUserId(User)) || User.IsInRole("Admin"))
        //    {

        //        // Construiește calea completă către imagine
        //        var filePath = Path.Combine(_env.WebRootPath, article.Image.TrimStart('/'));

        //        // Șterge imaginea dacă există
        //        if (System.IO.File.Exists(filePath))
        //        {
        //            System.IO.File.Delete(filePath);
        //        }

        //        db.Articles.Remove(article);
        //        db.SaveChanges();
        //        TempData["message"] = "Articolul a fost sters";
        //        TempData["messageType"] = "alert-success";
        //        return RedirectToAction("Index");
        //    }
        //    else
        //    {
        //        TempData["message"] = "Nu aveti dreptul sa faceti modificari asupra unui articol care nu va apartine";
        //        TempData["messageType"] = "alert-danger";

        //        return RedirectToAction("Index");
        //        //return RedirectToAction("Show", new { id });
        //    }
        //}



        // Conditiile de afisare pentru butoanele de editare si stergere
        // butoanele aflate in view-uri
        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("User"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.UserCurent = _userManager.GetUserId(User);

            ViewBag.EsteAdmin = User.IsInRole("Admin");
            ViewBag.EsteEditor = User.IsInRole("Editor");
        }




        [NonAction]
        public IEnumerable<SelectListItem> GetAllChapters()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var categories = from cat in db.Chapters
                             select cat;

            // iteram prin categorii
            foreach (var Chapter in categories)
            {
                // adaugam in lista elementele necesare pentru dropdown
                // id-ul categoriei si denumirea acesteia
                selectList.Add(new SelectListItem
                {
                    Value = Chapter.Id.ToString(),
                    Text = Chapter.ChapterTitle
                });
            }
            /* Sau se poate implementa astfel: 
             * 
            foreach (var Chapter in categories)
            {
                var listItem = new SelectListItem();
                listItem.Value = Chapter.Id.ToString();
                listItem.Text = Chapter.ChapterName;

                selectList.Add(listItem);
             }*/


            // returnam lista de categorii
            return selectList;
        }

        //public IActionResult ArticlesForSubjectAndGradeAndChapter(int subjectid, int gradeid, int chapterid)
        //{
        //    var articles = (from a in db.Articles
        //                   where a.ChapterId == chapterid
        //                   select a).ToList();

        //    ViewBag.Articles = articles;

        //    ViewBag.Grade = (from g in db.Grades
        //                     where g.Id == gradeid
        //                     select g).FirstOrDefault();

        //    ViewBag.Subject = (from s in db.Subjects
        //                       where s.Id == subjectid
        //                       select s).FirstOrDefault();

        //    ViewBag.Chapter = (from c in db.Chapters
        //                        where c.Id == chapterid
        //                        select c).FirstOrDefault();

        //    return View();
        //}


        public IActionResult ArticlesForSubjectAndGradeAndChapter(int subjectid, int gradeid, int chapterid, string sortOrder = "date_asc")
        {
            // Query cu toate relațiile necesare și filtrare după toate criteriile
            var articles = db.Articles
                            .Include(a => a.Chapter)
                                .ThenInclude(c => c.Grade)
                            .Include(a => a.Chapter)
                                .ThenInclude(c => c.Subject)
                            .Include(a => a.User)
                            .Where(a => a.ChapterId == chapterid
                                    && a.Chapter.GradeId == gradeid
                                    && a.Chapter.SubjectId == subjectid);



            

            // Setăm parametrii de sortare pentru View
            ViewData["DateSortParam"] = sortOrder == "date_asc" ? "date_desc" : "date_asc";
            ViewData["TitleSortParam"] = sortOrder == "title_asc" ? "title_desc" : "title_asc";

            // Aplicăm sortarea
            switch (sortOrder)
            {
                case "date_asc":
                    articles = articles.OrderBy(a => a.Date);
                    break;
                case "date_desc":
                    articles = articles.OrderByDescending(a => a.Date);
                    break;
                case "title_asc":
                    articles = articles.OrderBy(a => a.Title);
                    break;
                case "title_desc":
                    articles = articles.OrderByDescending(a => a.Title);
                    break;
                default:
                    articles = articles.OrderByDescending(a => a.Date);
                    break;
            }

            if (TempData.ContainsKey("message"))
            {
                ViewBag.Message = TempData["message"];
            }


            ViewBag.Articles = articles;




            //// MOTOR DE CAUTARE

            //var search = "";

            //if (Convert.ToString(HttpContext.Request.Query["search"]) != null)
            //{
            //    search = Convert.ToString(HttpContext.Request.Query["search"]).Trim(); // eliminam spatiile libere 

            //    // Cautare in articol (Title si Content)

            //    List<int> articleIds = db.Articles.Where
            //                            (
            //                             at => at.Title.Contains(search)
            //                             || at.Content.Contains(search)
            //                            ).Select(a => a.Id).ToList();

            //    // Cautare in comentarii (Content)
            //    List<int> articleIdsOfCommentsWithSearchString = db.Comments
            //                            .Where
            //                            (
            //                             c => c.Content.Contains(search)
            //                            ).Select(c => (int)c.ArticleId).ToList();

            //    // Se formeaza o singura lista formata din toate id-urile selectate anterior
            //    List<int> mergedIds = articleIds.Union(articleIdsOfCommentsWithSearchString).ToList();


            //    // Lista articolelor care contin cuvantul cautat
            //    // fie in articol -> Title si Content
            //    // fie in comentarii -> Content
            //    articles = db.Articles.Where(article => mergedIds.Contains(article.Id))
            //                          .Include("Chapter")
            //                          .Include("User")
            //                          .OrderByDescending(a => a.Date);

            //}

            //ViewBag.SearchString = search;

            //// AFISARE PAGINATA

            //// Alegem sa afisam 3 articole pe pagina
            //int _perPage = 3;

            //// Fiind un numar variabil de articole, verificam de fiecare data utilizand 
            //// metoda Count()

            //int totalItems = articles.Count();

            //// Se preia pagina curenta din View-ul asociat
            //// Numarul paginii este valoarea parametrului page din ruta
            //// /Articles/Index?page=valoare

            //var currentPage = Convert.ToInt32(HttpContext.Request.Query["page"]);

            //// Pentru prima pagina offsetul o sa fie zero
            //// Pentru pagina 2 o sa fie 3 
            //// Asadar offsetul este egal cu numarul de articole care au fost deja afisate pe paginile anterioare
            //var offset = 0;

            //// Se calculeaza offsetul in functie de numarul paginii la care suntem
            //if (!currentPage.Equals(0))
            //{
            //    offset = (currentPage - 1) * _perPage;
            //}

            //// Se preiau articolele corespunzatoare pentru fiecare pagina la care ne aflam 
            //// in functie de offset
            //var paginatedArticles = articles.Skip(offset).Take(_perPage);


            //// Preluam numarul ultimei pagini
            //ViewBag.lastPage = Math.Ceiling((float)totalItems / (float)_perPage);

            //// Trimitem articolele cu ajutorul unui ViewBag catre View-ul corespunzator
            //ViewBag.Articles = paginatedArticles;

            //// DACA AVEM AFISAREA PAGINATA IMPREUNA CU SEARCH

            //if (search != "")
            //{
            //    ViewBag.PaginationBaseUrl = "/Articles/Index/?search=" + search + "&page";
            //}
            //else
            //{
            //    ViewBag.PaginationBaseUrl = "/Articles/Index/?page";
            //}




            ViewBag.GradeId = gradeid;
            ViewBag.SubjectId = subjectid;
            ViewBag.ChapterId=chapterid;




            return View(articles.ToList());
        }



    }
}
