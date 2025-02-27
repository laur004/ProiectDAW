﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ProiectDAW.Data;
using ProiectDAW.Models;

namespace ProiectDAW.Controllers
{
    
    public class ChaptersController : Controller
    {


        // PASUL 10: useri si roluri 

        private readonly ApplicationDbContext db;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        public ChaptersController(
        ApplicationDbContext context,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager
        )
        {
            db = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }



        //private readonly ApplicationDbContext db;
        //public ChaptersController(ApplicationDbContext db) 
        //{
        //    this.db = db;
        //}

        [Authorize(Roles = "Admin")]
        public IActionResult Index()
        {
            var chapters = from chap in db.Chapters
                           orderby chap.ChapterTitle
                           select chap;

            ViewBag.Chapters = chapters;
            /*
            // Obține capitolele care aparțin unei materii și unei clase specifice
            var chapters = db.Chapters
                             .Include(c => c.Subject) // Include relația cu Subjects
                             //.Include(c => c.Grade)   // Include relația cu Grades
                             //.Where(c => c.Subject.Id == subjectId && c.Grade.Id == gradeId) // Filtrare
                             .ToList();

            // Trimite materia selectată și capitolele către View
            ViewBag.SubjectId = subjectId;
            ViewBag.GradeId = gradeId;

            return View(chapters);
            */

            return View();
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Show(int id)
        {
            Chapter chapter = db.Chapters.Find(id);
            return View(chapter);
        }

        [Authorize(Roles = "Admin")]
        public IActionResult New()
        {
            Chapter chapter = new Chapter();
            chapter.Subj = GetAllSubjects();
            //DUPA CE ADAUG CLASA
            chapter.Grad = GetAllGrades();

            return View(chapter);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult New(Chapter chapter)
        {
            if (ModelState.IsValid)
            {
                db.Chapters.Add(chapter);
                db.SaveChanges();
                TempData["message"] = "Capitolul a fost adaugat";
                return RedirectToAction("Index");
            }
            else
            {
                chapter.Subj = GetAllSubjects();
                chapter.Grad = GetAllGrades();
                return View(chapter);
            }

        }

        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id)
        {
            Chapter chapter = db.Chapters.Find(id);

            chapter.Subj = GetAllSubjects();
            chapter.Grad=GetAllGrades();
            return View(chapter);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Edit(int id, Chapter req_chapter)
        {
            Chapter chapter = db.Chapters.Find(id);

            if (ModelState.IsValid)
            {
                chapter.ChapterTitle = req_chapter.ChapterTitle;
                chapter.SubjectId = req_chapter.SubjectId;
                chapter.GradeId = req_chapter.GradeId;
                db.SaveChanges();
                TempData["message"] = "Capitolul a fost modificat!";
                return RedirectToAction("Index");

            }
            return View(req_chapter);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public IActionResult Delete(int id)
        {

            Chapter chapter = db.Chapters.Include("Articles")
                                             .Include("Articles.Comments")
                                             .Where(c => c.Id == id)
                                             .First();

            //Chapter chapter = db.Chapters.Find(id);
            db.Chapters.Remove(chapter);
            db.SaveChanges();
            TempData["message"] = "Capitolul a fost sters!";
            return RedirectToAction("Index");
        }



        




        [NonAction]
        public IEnumerable<SelectListItem> GetAllSubjects()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var subjects = from subj in db.Subjects
                             select subj;

            // iteram prin subiecte
            
            foreach (var subject in subjects)
            {
                var listItem = new SelectListItem();
                listItem.Value = subject.Id.ToString();
                listItem.Text = subject.Name;

                selectList.Add(listItem);
             }


            // returnam lista de categorii
            return selectList;
        }


        //DUPA CE ADAUG CLASA
        [NonAction]
        public IEnumerable<SelectListItem> GetAllGrades()
        {
            // generam o lista de tipul SelectListItem fara elemente
            var selectList = new List<SelectListItem>();

            // extragem toate categoriile din baza de date
            var grades = from grad in db.Grades
                           select grad;

            // iteram prin clase

            foreach (var grade in grades)
            {
                var listItem = new SelectListItem();
                listItem.Value = grade.Id.ToString();
                listItem.Text = grade.GradeName;

                selectList.Add(listItem);
            }


            // returnam lista de categorii
            return selectList;
        
        }

        public IActionResult ChaptersForSubjectAndGrade(int subjectid, int gradeid)
        {
            var chapters = (from c in db.Chapters
                           where c.SubjectId == subjectid && c.GradeId == gradeid
                           select c).ToList();

            ViewBag.Chapters = chapters;

            ViewBag.Grade = (from g in db.Grades
                               where g.Id == gradeid
                               select g).FirstOrDefault();

            ViewBag.Subject = (from s in db.Subjects
                               where s.Id == subjectid
                               select s).FirstOrDefault();

            SetAccessRights();

            return View();
        }






        private void SetAccessRights()
        {
            ViewBag.AfisareButoane = false;

            if (User.IsInRole("User"))
            {
                ViewBag.AfisareButoane = true;
            }

            ViewBag.UserCurent = _userManager.GetUserId(User);

            ViewBag.EsteAdmin = User.IsInRole("Admin");
        }

    }
}
