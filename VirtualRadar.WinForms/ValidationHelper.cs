﻿// Copyright © 2010 onwards, Andrew Whewell
// All rights reserved.
//
// Redistribution and use of this software in source and binary forms, with or without modification, are permitted provided that the following conditions are met:
//    * Redistributions of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
//    * Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials provided with the distribution.
//    * Neither the name of the author nor the names of the program's contributors may be used to endorse or promote products derived from this software without specific prior written permission.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHORS OF THE SOFTWARE BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VirtualRadar.Interface.View;
using System.Windows.Forms;

namespace VirtualRadar.WinForms
{
    /// <summary>
    /// An object that helps views display validation results correctly.
    /// </summary>
    class ValidationHelper
    {
        /// <summary>
        /// A map of validation fields to the corresponding controls.
        /// </summary>
        private Dictionary<ValidationField, Control> _ValidationFieldMap = new Dictionary<ValidationField,Control>();

        /// <summary>
        /// The error provider that will be used to display validation results.
        /// </summary>
        private ErrorProvider _ErrorProvider;

        /// <summary>
        /// The error provider that will be used to display warnings to the user.
        /// </summary>
        private ErrorProvider _WarningProvider;

        /// <summary>
        /// Creates a new object.
        /// </summary>
        /// <param name="errorProvider"></param>
        /// <param name="warningProvider"></param>
        public ValidationHelper(ErrorProvider errorProvider, ErrorProvider warningProvider = null)
        {
            _ErrorProvider = errorProvider;
            _WarningProvider = warningProvider;
        }

        /// <summary>
        /// Tells the helper which fields the view knows about and which control corresponds to which field.
        /// </summary>
        /// <param name="validationField"></param>
        /// <param name="control"></param>
        /// <remarks>
        /// It is not permissable to register the same field or control twice.
        /// </remarks>
        public void RegisterValidationField(ValidationField validationField, Control control)
        {
            if(control == null) throw new ArgumentNullException("control");
            if(_ValidationFieldMap.ContainsKey(validationField)) throw new InvalidOperationException(String.Format("An attempt was made to register the {0} validation field twice", validationField));
            if(_ValidationFieldMap.Where(kvp => kvp.Value == control).Any()) throw new InvalidOperationException(String.Format("An attempt was made to register the {0} control twice", control.Name));

            _ValidationFieldMap.Add(validationField, control);
        }

        /// <summary>
        /// Displays a set of validation results.
        /// </summary>
        /// <param name="validationResults"></param>
        public void ShowValidationResults(IEnumerable<ValidationResult> validationResults)
        {
            ClearAllMessages();

            foreach(var validationResult in validationResults) {
                Control control;
                if(_ValidationFieldMap.TryGetValue(validationResult.Field, out control)) {
                    var errorProvider = validationResult.IsWarning ? _WarningProvider : _ErrorProvider;
                    errorProvider.SetError(control, validationResult.Message);
                }
            }
        }

        /// <summary>
        /// Removes all error messages.
        /// </summary>
        private void ClearAllMessages()
        {
            foreach(var kvp in _ValidationFieldMap) {
                if(_ErrorProvider != null)      _ErrorProvider.SetError(kvp.Value, null);
                if(_WarningProvider != null)    _WarningProvider.SetError(kvp.Value, null);
            }
        }
    }
}
