using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;
using System.Net.Http;
using System.Collections.Generic;
using System.Web;
using System.Xml;

namespace TranslationBot.Dialogs
{
    [Serializable]
    public class TranslateDialog : IDialog<object>
    {
        private string translatorToken;
        private string translationLanguage;
        private string language;

        public async Task StartAsync(IDialogContext context)
        {
            await GetTokenAsync();
            await context.PostAsync("In welche Sprache soll ich übersetzen?");
            ChoseLanguage(context);
        }

        /// <summary>
        /// Gets the token for using the translator API
        /// More on http://docs.microsofttranslator.com/oauth-token.html
        /// You will need a key for the Microsoft translator API: https://azure.microsoft.com/en-us/services/cognitive-services/translator-text-api
        /// </summary>
        /// <returns></returns>
        private async Task GetTokenAsync()
        {
            var client = new HttpClient();

            client.BaseAddress = new Uri("https://api.cognitive.microsoft.com/sts/v1.0/");

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "enterkeyhere");

            var content = new StringContent("{}");
            //content.Headers.Add("Content-Type", "application/json");

            var response = await client.PostAsync("issueToken", content);

            translatorToken = await response.Content.ReadAsStringAsync();
        }

        private void ChoseLanguage(IDialogContext context)
        {
            //Initiate a dialog and let the user chose between different languages to translate to
            PromptDialog.Choice(context, SetLanguage,
                new List<string>
                {
                    "Englisch",
                    "Japanisch",
                    "Spanisch"
                },
                "In welche Sprache soll ich übersetzen?");
        }

        private async Task SetLanguage(IDialogContext context, IAwaitable<string> result)
        {
            language = await result;

            switch (language)
            {
                case "Englisch":
                    translationLanguage = "en";
                    break;
                case "Japanisch":
                    translationLanguage = "ja";
                    break;
                case "Spanisch":
                    translationLanguage = "es";
                    break;
                default:
                    translationLanguage = "en";
                    break;
            }

            await context.PostAsync($"Was möchtest auf {language} übersetzen?");

            context.Wait(TranslateAsync);
        }

        /// <summary>
        /// Translates through the Microsoft Translator API
        /// More can be found at http://docs.microsofttranslator.com/text-translate.html#/
        /// </summary>
        /// <param name="context"></param>
        /// <param name="result"></param>
        /// <returns></returns>
        private async Task TranslateAsync(IDialogContext context, IAwaitable<object> result)
        {
            var typingMessage = context.MakeMessage();
            typingMessage.Type = ActivityTypes.Typing;
            await context.PostAsync(typingMessage);

            var activity = await result as Activity;

            var translationText = activity.Text;

            if (translatorToken == null)
            {
                await GetTokenAsync();
            }

            var client = new HttpClient();

            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {translatorToken}");
            client.DefaultRequestHeaders.Add("Accept", "application/xml");

            var query = HttpUtility.ParseQueryString(string.Empty);
            query["text"] = translationText;
            query["to"] = translationLanguage;

            var builder = new UriBuilder("https://api.microsofttranslator.com/v2/http.svc/Translate");

            builder.Query = query.ToString();

            var response = await client.GetAsync(builder.ToString());

            if (response.StatusCode == System.Net.HttpStatusCode.OK)
            {
                var text = await response.Content.ReadAsStringAsync();

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(text);

                var translatedText = doc.FirstChild.InnerText;

                await context.PostAsync($"\"{translationText}\" auf *{language}* ist: \"{translatedText}\". Ich hoffe das hilft dir weiter.");
            }
            else await context.PostAsync("Es tut mir leid, aber das konnte ich leider nicht übersetzen. Versuche etwas anderes.");

            await context.PostAsync("In welche Sprache möchtest du als nächstes übersetzen?");

            ChoseLanguage(context);

            //context.Wait(TranslateAsync);
            //context.Done(context);
        }
    }
}