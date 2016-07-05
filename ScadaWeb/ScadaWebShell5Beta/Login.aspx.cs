﻿/*
 * Copyright 2016 Mikhail Shiryaev
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 * 
 * 
 * Product  : Rapid SCADA
 * Module   : SCADA-Web
 * Summary  : Login web form
 * 
 * Author   : Mikhail Shiryaev
 * Created  : 2016
 * Modified : 2016
 */

using Scada.UI;
using System;
using System.IO;

namespace Scada.Web
{
    /// <summary>
    /// Login web form
    /// <para>Веб-форма входа в систему</para>
    /// </summary>
    public partial class WFrmLogin : System.Web.UI.Page
    {
        /// <summary>
        /// Стартовая страница по умолчанию
        /// </summary>
        private const string DefaultStartPage = "~/View.aspx";

        private AppData appData;   // общие данные веб-приложения
        private UserData userData; // данные пользователя приложения


        /// <summary>
        /// Добавить на страницу скрипт вывода сообщения об ошибке
        /// </summary>
        private void AddShowAlertScript(string message)
        {
            ClientScript.RegisterStartupScript(GetType(), "Startup1", "showAlert('" + message + "');", true);
        }

        /// <summary>
        /// Добавить на страницу скрипт проверки браузера
        /// </summary>
        private void AddCheckBrowserScript()
        {
            ClientScript.RegisterStartupScript(GetType(), "Startup2", "checkBrowser();", true);
        }

        /// <summary>
        /// Получить адрес стартовой страницы на основе адреса из настроек
        /// </summary>
        private string GetStartPageUrl(string startPage)
        {
            startPage = startPage == null ? "" : startPage.Trim();

            if (startPage == "")
            {
                return DefaultStartPage;
            }
            else
            {
                int ind = startPage.IndexOf('?');
                string virtPath = ind >= 0 ? startPage.Substring(0, ind) : startPage;
                string physPath = Server.MapPath(virtPath);
                return File.Exists(physPath) ? startPage : DefaultStartPage;
            }
        }

        /// <summary>
        /// Выбор стартовой страницы и переход на неё
        /// </summary>
        private void GoToStartPage()
        {
            string returnUrl = Request.QueryString["return"];
            if (string.IsNullOrEmpty(returnUrl))
                Response.Redirect(GetStartPageUrl(userData.WebSettings.StartPage));
            else
                Response.Redirect(returnUrl);
        }


        protected void Page_Load(object sender, EventArgs e)
        {
            appData = AppData.GetAppData();
            userData = UserData.GetUserData();

            if (IsPostBack)
            {
                Title = (string)ViewState["Title"];
            }
            else
            {
                // перевод веб-страницы
                Translator.TranslatePage(this, "Scada.Web.WFrmLogin");
                ViewState["Title"] = Title;

                // вывод сообщения, заданного в параметрах запроса
                string alert = Request.QueryString["alert"];
                bool alertIsEmpty = string.IsNullOrEmpty(alert);
                if (!alertIsEmpty)
                    AddShowAlertScript(alert);

                // обработка сохранённого входа в систему
                if (alertIsEmpty && !userData.LoggedOn && userData.WebSettings.RemEnabled)
                {
                    string username;
                    if (appData.RememberMe.ValidateUser(Context, out username, out alert) && 
                        userData.Login(username, out alert))
                        GoToStartPage();
                    else if (alert != "")
                        AddShowAlertScript(alert);
                }

                // настройка элементов управления
                pnlRememberMe.Visible = userData.WebSettings.RemEnabled;
                txtUsername.Text = userData.LoggedOn ?
                    userData.UserName :
                    appData.RememberMe.RestoreUsername(Context); // из cookie

                // добавление скрипта проверки браузера
                AddCheckBrowserScript();
            }
        }

        protected void btnLogin_Click(object sender, EventArgs e)
        {
            string errMsg;

            if (userData.Login(txtUsername.Text, txtPassword.Text, out errMsg))
            {
                // сохранение информации о входе пользователя
                if (chkRememberMe.Checked)
                    appData.RememberMe.RememberUser(userData.UserName, Context);
                else
                    appData.RememberMe.RememberUsername(userData.UserName, Context);

                // переход на стартовую страницу
                GoToStartPage();
            }
            else
            {
                AddShowAlertScript(errMsg);
            }
        }
    }
}