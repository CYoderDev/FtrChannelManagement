using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrontierVOps.FiOS.HealthCheck.DataObjects
{
    public class HTMLFormatter
    {
        private StringBuilder _strBuilder { get; set; }
        private bool _isTableStarted { get; set; }
        private bool _isLastRowError { get; set; }

        public HTMLFormatter()
        {
            _strBuilder = new StringBuilder();
            _isTableStarted = false;
            _isLastRowError = false;
        }

        public void SetRole(string Role)
        {
            _strBuilder.AppendLine("<p style=\"font-size: 150%; font-weight: bold; font-variant: small-caps; padding: 4px;\">");
            _strBuilder.AppendFormat("<u>{0}</u>", Role.ToLower());
            _strBuilder.AppendLine("</p>");
        }

        public void BeginTable(string ServerName)
        {
            _strBuilder.AppendLine("<div><br /></div>");
            _strBuilder.AppendLine("<table border=\"0\" style=\"border-collapse: collapse; width: 65%;\">");
            _strBuilder.AppendLine("<th colspan=\"3\" style=\"font-size: 110%; padding: 4px; font-variant: small-caps; background: black; color: white;\">");
            _strBuilder.Append(ServerName.ToLower());
            _strBuilder.AppendLine("</th>");
            this._isTableStarted = true;
        }

        public void AddStatusRow(string CheckName, StatusResult Result)
        {
            checkIsTableStarted();

            _strBuilder.AppendLine("<tr style=\"border: 1px solid black\">");
            _strBuilder.AppendLine("<td style=\"padding: 1px; border: 1px solid black\">");
            _strBuilder.Append(CheckName);
            _strBuilder.AppendLine("</td>");
            _strBuilder.Append("<td style=\"border: 1px solid black; padding: 2px; background:");

            switch (Result)
            {
                case StatusResult.Ok:
                    _strBuilder.Append("#4bc659;\">");
                    this._isLastRowError = false;
                    break;
                case StatusResult.Warning:
                    _strBuilder.Append("#efad43;\">");
                    this._isLastRowError = false;
                    break;
                case StatusResult.Error:
                    _strBuilder.Append("#c64b4b;\">");
                    this._isLastRowError = true;
                    break;
                case StatusResult.Critical:
                    _strBuilder.Append("#e50b0b; font-weight: bold;\">");
                    this._isLastRowError = true;
                    break;
            }

            _strBuilder.AppendLine(Result.ToString());
            _strBuilder.AppendLine("</td></tr>");
        }

        public void AddErrorDescriptionRows(List<string> Errors)
        {
            checkIsTableStarted();
            checkIsLastRowError();

            _strBuilder.AppendLine("<tr>");
            _strBuilder.AppendLine("<td colspan=\"3\">");
            _strBuilder.AppendLine("<table style=\"font-size: 80%; background: #bcb495; width: 80%\">");
            _strBuilder.AppendLine("<tr><td style=\"color:#e50b0b\"><ul>");

            Errors.ForEach((err) =>
                {
                    _strBuilder.AppendFormat("<li>{0}</li>", err);
                });

            _strBuilder.AppendLine("</ul></td></tr></table></td></tr>");
        }

        public void EndTable()
        {
            _strBuilder.AppendLine("</table>");
            this._isTableStarted = false;
        }

        public override string ToString()
        {
            if (this._isTableStarted)
                throw new Exception("Cannot format to string without closing the open table by calling EndTable method.");
            return _strBuilder.ToString();
        }

        #region VerificationMethods
        private void checkIsTableStarted()
        {
            if (!this._isTableStarted)
                throw new Exception("Table has not yet been created. Run BeginTable method.");
        }

        private void checkIsLastRowError()
        {
            if (!this._isLastRowError)
                throw new Exception("Cannot add error description. The previous row was not in error or critical status.");
        }
        #endregion VerificationMethods
    }
}
