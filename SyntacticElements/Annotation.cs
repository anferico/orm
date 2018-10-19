﻿using System;
using System.Collections.Generic;
namespace AnnotationsProject {
	public abstract class Annotation {
		public string AnnotationName;
		public Dictionary<string, string> Attributes;

		protected Annotation(string annotationName) {
			AnnotationName = annotationName;
			Attributes = new Dictionary<string, string>();
		}
	}
}
