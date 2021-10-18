﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PhoDiem_TLU.Commons;
using PhoDiem_TLU.Models;
using PhoDiem_TLU.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace PhoDiem_TLU.Core
{
    public class LoginTokenHttpClient
    {
        //url đăng nhập
        private static readonly string url = "http://sv313.tlu.edu.vn:8080/education/oauth/token";
        //url lấy user ứng với token
        private static readonly string urlGet = "http://sv313.tlu.edu.vn:8080/education/api/users/getCurrentUser";
        public static TokenResult _token = null;
        
        public static async Task<TokenResult> LoginDTSAsync(UserLoginViewModel model)
        {
            //Dùng HTTPClient để giao tiếp với server
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            //Tạo model chứa thông tin login
            var user = new UserLoginViewModel()
            {
                client_id = "education_client",
                grant_type = "password",
                username = model.username.ToString(),
                password = model.password.ToString(),
                client_secret = "password"
            };

            //Convert ra json
            string json = await Task.Run(() => JsonConvert.SerializeObject(user));
            var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

            httpContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
            //Send request lên server
            HttpResponseMessage message = await client.PostAsync(string.Format(url), httpContent);
            //Nếu IsSuccessStatusCode = 200 thì sẽ trả ra token, sau đó lấy token gán vào session
            if (message.IsSuccessStatusCode)
            {
                var responseData = message.Content.ReadAsStringAsync().Result;
                string barearToken = responseData;
                string[] result = barearToken.Split(',');
                _token = TokenResult.handleStringToToken(result);
                return _token;
            }
            return null;
        }

        public static bool GetCurrentUser(TokenResult tokenResult)
        {
            //Tương tự như hàm trên sử dụng httpclient để giao tiếp với server
            using (var client = new HttpClient())
            {
                //Gán header để Authorization với Bearer Token
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                        tokenResult.access_token);

                var result = client.GetAsync(urlGet).Result.Content.ReadAsStringAsync();

                if (result != null)
                {
                    bool check = handleRoles(result.Result);
                    return check;
                }
            }
            return false;
        }

        private static bool handleRoles(string jsoninput)
        {
            JObject json = JObject.Parse(jsoninput);
            var jsonResult = json.Root;
            var roles = jsonResult["roles"];
            if (roles != null)
            {
                List<RoleViewModel> resultRoles = RoleViewModel.getRoles(roles);
                foreach(RoleViewModel item in resultRoles)
                {
                    if(item.name == Constants.ROLE_EXAM_MANAGERMENT)
                    {
                        return true;
                    }
                }
            }
            return false;
        }
       
    }
}