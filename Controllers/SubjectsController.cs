﻿using Microsoft.AspNetCore.Mvc;
using ProiectDAW.Data;
using ProiectDAW.Models;

namespace ProiectDAW.Controllers
{
    public class SubjectsController : Controller
    {
        private readonly ApplicationDbContext db;

        public SubjectsController(ApplicationDbContext db)
        {
            this.db = db;
        }

        public IActionResult Index()
        {
            var subjects = from subj in db.Subjects
                           orderby subj.Name
                           select subj;

            ViewBag.Subjects = subjects;

            return View();
        }

        public IActionResult Show(int id) 
        {
            Subject subject= db.Subjects.Find(id);
            return View(subject);
        }

        public IActionResult New()
        {
            return View();
        }

        [HttpPost]
        public IActionResult New(Subject subject)
        {
            if (ModelState.IsValid)
            {
                db.Subjects.Add(subject);
                db.SaveChanges();
                TempData["message"] = "Materia a fost adaugata";
                return RedirectToAction("Index");
            }
            else
            {
                return View(subject);
            }
            
        }

        public IActionResult Edit(int id)
        {   
            Subject subject = db.Subjects.Find(id);
            return View(subject);
        }

        [HttpPost]
        public IActionResult Edit(int id, Subject req_subject)
        {
            Subject subject = db.Subjects.Find(id);

            if (ModelState.IsValid)
            {
                subject.Name = req_subject.Name;
                db.SaveChanges();
                TempData["message"] = "Materia a fost modificata!";
                return RedirectToAction("Index");

            }
            return View(req_subject);
        }

        [HttpPost]
        public IActionResult Delete(int id)
        {
            Subject subject = db.Subjects.Find(id);
            db.Subjects.Remove(subject);
            db.SaveChanges();
            TempData["message"] = "Materia a fost stearsa!";
            return RedirectToAction("Index");
        }
    }
}
