using System;
using System.Collections.Generic;

namespace CodeGeneration 
{
    public class TableAnnotation : Annotation 
    {
        public string InterfaceModifier;
        public string InterfaceName;
        public List<MemberAnnotation> MemberAnnotations;

        public TableAnnotation(
            string annotationName, 
            string interfaceModifier,
            string interfaceName
        ) : base(annotationName) {
            InterfaceModifier = interfaceModifier;
            InterfaceName = interfaceName;
            MemberAnnotations = new List<MemberAnnotation>();
        }

    }
}
