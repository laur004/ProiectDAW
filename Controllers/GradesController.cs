﻿using Microsoft.AspNetCore.Mvc;
using ProiectDAW.Data;
using ProiectDAW.Models;

namespace ProiectDAW.Controllers
{
	public class GradesController : Controller
	{
		private readonly ApplicationDbContext db;

		public GradesController(ApplicationDbContext db)
		{
			this.db = db;
		}

		public IActionResult Index()
		{
			var Grades = from grad in db.Grades
						   orderby grad.GradeName
						   select grad;

			ViewBag.Grades = Grades;

			return View();
		}

		public IActionResult Show(int id)
		{
			Grade grade = db.Grades.Find(id);
			return View(grade);
		}

		public IActionResult New()
		{
			return View();
		}

		[HttpPost]
		public IActionResult New(Grade grade)
		{
			if (ModelState.IsValid)
			{
				db.Grades.Add(grade);
				db.SaveChanges();
				TempData["message"] = "Clasa a fost adaugata";
				return RedirectToAction("Index");
			}
			else
			{
				return View(grade);
			}

		}

		public IActionResult Edit(int id)
		{
			Grade grade = db.Grades.Find(id);
			return View(grade);
		}

		[HttpPost]
		public IActionResult Edit(int id, Grade req_grade)
		{
			Grade grade = db.Grades.Find(id);

			if (ModelState.IsValid)
			{
				grade.GradeName = req_grade.GradeName;
				db.SaveChanges();
				TempData["message"] = "clasa a fost modificata!";
				return RedirectToAction("Index");

			}
			return View(req_grade);
		}

		[HttpPost]
		public IActionResult Delete(int id)
		{
			Grade grade = db.Grades.Find(id);
			db.Grades.Remove(grade);
			db.SaveChanges();
			TempData["message"] = "clasa a fost stearsa!";
			return RedirectToAction("Index");
		}
	}
}