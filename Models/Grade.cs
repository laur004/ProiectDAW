﻿using System.ComponentModel.DataAnnotations;

namespace ProiectDAW.Models
{
	public class Grade
	{
		public int Id { get; set; }

		[Required(ErrorMessage = "Numele clasei este obligatoriu")]
		[StringLength(100,ErrorMessage = "Lungimea maxima trebuie sa fie 100")]
        public string GradeName { get; set; }

		public virtual ICollection<Chapter>? Chapters { get; set; }
	}
}
