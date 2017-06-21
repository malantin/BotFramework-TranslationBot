using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Connector;

namespace TranslationBot.Dialogs
{
    [Serializable]
    public class RootDialog : IDialog<object>
    {
        public Task StartAsync(IDialogContext context)
        {
            context.Wait(MessageReceivedAsync);

            return Task.CompletedTask;
        }

        private async Task MessageReceivedAsync(IDialogContext context, IAwaitable<object> result)
        {

            await context.PostAsync("Hi, ich bin dein Übersetzungsbot. Lass uns loslegen.");

            context.Call<object>(new TranslateDialog(), MessageReceivedAsync);

            //context.Wait(MessageReceivedAsync);
        }
    }
}