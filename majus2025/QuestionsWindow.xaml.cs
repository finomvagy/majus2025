using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media; // Brushes miatt
using Microsoft.VisualBasic; // InputBox miatt

namespace majus2025
{
    public partial class QuestionsWindow : Window
    {
        private readonly int quizId;
        // private readonly string jwtToken; // HttpClient már tartalmazza
        private const string BaseUrl = "http://localhost:5555";
        private readonly HttpClient _httpClient;

        public QuestionsWindow(int quizId, string jwtToken)
        {
            InitializeComponent();
            this.quizId = quizId;
            // this.jwtToken = jwtToken; // Nem szükséges tárolni, ha a _httpClient-ben van
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Authorization =
                new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);
            Loaded += async (_, __) => await LoadQuestions();
        }

        public class Question
        {
            public int id { get; set; }
            public int quizId { get; set; }
            public string question_text { get; set; }
            // A válaszokat a LoadQuestions-ben külön töltjük be, vagy a backend adja a kérdéssel együtt
            // Ha a backend a GET /quizzes/:quizId/questions végponton adja a válaszokat is, akkor itt is lehetne egy List<Answer>
        }

        public class Answer
        {
            public int id { get; set; }
            public string text { get; set; }
            public bool isCorrect { get; set; }
            // public int questionId { get; set; } // Szükség esetén
        }

        private async Task<List<Answer>> LoadAnswersForQuestion(int questionId)
        {
            try
            {
                var response = await _httpClient.GetAsync($"{BaseUrl}/questions/{questionId}/answers");
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<Answer>>(json) ?? new List<Answer>();
                }
                else
                {
                    MessageBox.Show($"Hiba a válaszok lekérésekor (kérdés ID: {questionId}): {await response.Content.ReadAsStringAsync()}", "Válasz Lekérési Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return new List<Answer>();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kritikus hiba a válaszok lekérésekor: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Answer>();
            }
        }

        private async Task LoadQuestions()
        {
            spQuestions.Children.Clear();
            try
            {
                // A backend /quizzes/:quizId/questions végpontja már tartalmazza a válaszokat is az include miatt.
                // Ezért a LoadAnswersForQuestion külön hívása nem feltétlenül szükséges itt, ha a Question osztály tartalmazza a válaszokat.
                // De ha a backend API struktúra ezt igényli, akkor a jelenlegi megközelítés (külön LoadAnswersForQuestion) is jó.
                // Tegyük fel, hogy a backend a kérdésekkel együtt adja a válaszokat:
                // Módosított Question osztály kellene: public List<Answer> answers { get; set; }
                // Jelenleg a kódod külön kéri le a válaszokat, ezt követem.

                var response = await _httpClient.GetAsync($"{BaseUrl}/quizzes/{quizId}/questions");
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show($"Hiba a kérdések lekérésekor: {await response.Content.ReadAsStringAsync()}", "Kérdés Lekérési Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                var json = await response.Content.ReadAsStringAsync();
                var questions = JsonConvert.DeserializeObject<List<Question>>(json);

                if (questions == null || questions.Count == 0)
                {
                    spQuestions.Children.Add(new TextBlock { Text = "Nincsenek kérdések ehhez a kvízhez.", Foreground = Brushes.LightGray, Margin = new Thickness(5) });
                    return;
                }

                foreach (var q in questions)
                {
                    var border = new Border
                    {
                        Background = Brushes.DimGray,
                        CornerRadius = new CornerRadius(8),
                        Margin = new Thickness(5),
                        Padding = new Thickness(10)
                    };

                    var stack = new StackPanel();
                    var txt = new TextBlock
                    {
                        Text = q.question_text,
                        Foreground = Brushes.White,
                        FontSize = 14,
                        TextWrapping = TextWrapping.Wrap
                    };
                    stack.Children.Add(txt);

                    // Szerkesztés és Törlés gombok
                    var spQuestionButtons = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 5, 0, 0) };
                    var btnEdit = new Button { Content = "Szerkesztés", Width = 80, Margin = new Thickness(0, 0, 5, 0), Tag = q };
                    btnEdit.Click += BtnEditQuestion_Click;
                    spQuestionButtons.Children.Add(btnEdit);

                    var btnDelete = new Button { Content = "Törlés", Width = 80, Tag = q.id };
                    btnDelete.Click += BtnDeleteQuestion_Click;
                    spQuestionButtons.Children.Add(btnDelete);
                    stack.Children.Add(spQuestionButtons);


                    // Válaszok betöltése és megjelenítése
                    var answers = await LoadAnswersForQuestion(q.id); // Külön hívás
                    if (answers.Any())
                    {
                        var answersGroupBox = new GroupBox
                        {
                            Header = "Válaszok:",
                            Foreground = Brushes.LightGray,
                            Margin = new Thickness(0, 10, 0, 0),
                            Padding = new Thickness(5)
                        };
                        var spAnswersInGroup = new StackPanel();
                        foreach (var a in answers)
                        {
                            var ansText = new TextBlock
                            {
                                Text = $"- {a.text}{(a.isCorrect ? " (Helyes)" : "")}",
                                Foreground = a.isCorrect ? Brushes.LightGreen : Brushes.LightSalmon,
                                FontSize = 12,
                                TextWrapping = TextWrapping.Wrap,
                                Margin = new Thickness(0, 2, 0, 2)
                            };
                            spAnswersInGroup.Children.Add(ansText);
                        }
                        answersGroupBox.Content = spAnswersInGroup;
                        stack.Children.Add(answersGroupBox);
                    }
                    else
                    {
                        stack.Children.Add(new TextBlock { Text = "Nincsenek válaszok ehhez a kérdéshez.", Foreground = Brushes.Orange, Margin = new Thickness(0, 5, 0, 0) });
                    }

                    // Új válaszok hozzáadása gomb (ha még nincsenek, vagy mindig engedélyezzük?)
                    // A jelenlegi logika szerint csak akkor jelenik meg, ha 0 válasz van.
                    // Lehetne egy "Válaszok kezelése" gomb, ami új ablakot nyit, vagy itt helyben engedi szerkeszteni/hozzáadni.
                    // A jelenlegi CSV-s megoldás egyszerűsítésként elfogadható.
                    // if (!answers.Any()) // Vagy mindig látható, ha újabb válaszokat akarunk hozzáadni
                    // {
                    var btnAddAnswers = new Button
                    {
                        Content = answers.Any() ? "További válaszok hozzáadása (CSV)" : "Válaszok hozzáadása (CSV)",
                        ToolTip = "Add meg a 4 választ vesszővel elválasztva, majd a helyeset.",
                        Width = 200,
                        Margin = new Thickness(0, 10, 0, 0)
                    };
                    btnAddAnswers.Click += async (_, __) => await AddAnswersViaCsvInput(q.id);
                    stack.Children.Add(btnAddAnswers);
                    // }


                    border.Child = stack;
                    spQuestions.Children.Add(border);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kritikus hiba a kérdések betöltésekor: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnEditQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is Question q)) return;

            string newText = Interaction.InputBox("Új kérdésszöveg:", "Kérdés szerkesztése", q.question_text);
            if (string.IsNullOrWhiteSpace(newText) || newText == q.question_text) return;

            var body = new { question_text = newText };
            var content = new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json");
            try
            {
                var putResp = await _httpClient.PutAsync($"{BaseUrl}/questions/{q.id}", content);
                if (putResp.IsSuccessStatusCode)
                {
                    await LoadQuestions();
                }
                else
                {
                    MessageBox.Show($"Hiba a kérdés frissítésekor: {await putResp.Content.ReadAsStringAsync()}", "Frissítési Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kritikus hiba: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnDeleteQuestion_Click(object sender, RoutedEventArgs e)
        {
            if (!((sender as Button)?.Tag is int questionId)) return;

            var result = MessageBox.Show("Biztosan törölni szeretnéd ezt a kérdést és minden hozzá tartozó választ?", "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var deleteResp = await _httpClient.DeleteAsync($"{BaseUrl}/questions/{questionId}");
                if (deleteResp.IsSuccessStatusCode)
                {
                    await LoadQuestions();
                }
                else
                {
                    MessageBox.Show($"Hiba a kérdés törlésekor: {await deleteResp.Content.ReadAsStringAsync()}", "Törlési Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kritikus hiba: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async Task AddAnswersViaCsvInput(int questionId)
        {
            string csvAnswers = Interaction.InputBox(
                "Írd be a (maximum 4) választ, vesszővel elválasztva:\n" +
                "Példa: alma, körte, banán, szilva",
                "Válaszok hozzáadása", "");
            if (string.IsNullOrWhiteSpace(csvAnswers)) return;

            var answerTexts = csvAnswers
                .Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(p => p.Trim())
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Take(4) // Maximum 4 választ engedélyezünk ezzel a módszerrel
                .ToList();

            if (!answerTexts.Any())
            {
                MessageBox.Show("Nem adott meg érvényes válaszokat.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (answerTexts.Count > 4)
            {
                MessageBox.Show("Maximum 4 választ adhat meg ezzel a módszerrel.", "Figyelmeztetés", MessageBoxButton.OK, MessageBoxImage.Warning);
            }


            string correctAnswerText = Interaction.InputBox(
                $"Válaszd ki a helyes választ az alábbiak közül (írd be pontosan):\n{string.Join("\n", answerTexts.Select(t => $"- {t}"))}",
                "Helyes válasz kiválasztása", answerTexts.FirstOrDefault() ?? "");

            if (string.IsNullOrWhiteSpace(correctAnswerText))
            {
                MessageBox.Show("Meg kell adni a helyes választ.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            correctAnswerText = correctAnswerText.Trim();
            if (!answerTexts.Contains(correctAnswerText, StringComparer.OrdinalIgnoreCase)) // Figyeljünk a kis/nagybetűkre, vagy ne
            {
                MessageBox.Show("A megadott helyes válasz nem szerepel a válaszlehetőségek között.", "Hiba", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // A backend API egy helyes választ engedélyez kérdésenként, és automatikusan kezeli a többi false-ra állítását.
            // Tehát elég csak azt elküldeni, amelyik helyes, a többit pedig isCorrect: false-szal.
            // Vagy, ha a backend úgy van megírva, hogy az új helyes válasz hozzáadásakor a régieket false-ra állítja, akkor elég csak a helyeset true-val küldeni.
            // A jelenlegi backend (ha jól emlékszem a korábbiakból) az új válasz hozzáadásakor, ha isCorrect=true, akkor a többit false-ra állítja.
            // Ezért itt minden választ elküldünk a megfelelő isCorrect flaggel.

            bool success = true;
            foreach (var answerText in answerTexts)
            {
                bool isThisOneCorrect = answerText.Equals(correctAnswerText, StringComparison.OrdinalIgnoreCase);
                var answerPayload = new { text = answerText, isCorrect = isThisOneCorrect };
                var content = new StringContent(JsonConvert.SerializeObject(answerPayload), Encoding.UTF8, "application/json");
                try
                {
                    var postResp = await _httpClient.PostAsync($"{BaseUrl}/questions/{questionId}/answers", content);
                    if (!postResp.IsSuccessStatusCode)
                    {
                        MessageBox.Show($"Hiba a(z) '{answerText}' válasz hozzáadásakor: {await postResp.Content.ReadAsStringAsync()}", "Válasz Mentési Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                        success = false;
                        // Lehetne itt break is, vagy folytatni a többi válasz mentését.
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Kritikus hiba a(z) '{answerText}' válasz hozzáadásakor: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                    success = false;
                }
            }

            if (success)
            {
                MessageBox.Show("Válaszok sikeresen hozzáadva/frissítve.", "Siker", MessageBoxButton.OK, MessageBoxImage.Information);
                await LoadQuestions(); // Frissítjük a listát, hogy látszódjanak az új válaszok
            }
        }


        private async void BtnAddQuestion_Click(object sender, RoutedEventArgs e)
        {
            string questionText = tbQuestionText.Text.Trim();
            if (string.IsNullOrWhiteSpace(questionText))
            {
                MessageBox.Show("A kérdés szövegét meg kell adni!", "Hiányzó Adat", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var payload = new { question_text = questionText };
            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            try
            {
                var response = await _httpClient.PostAsync($"{BaseUrl}/quizzes/{quizId}/questions", content);
                if (response.IsSuccessStatusCode)
                {
                    tbQuestionText.Text = ""; // Mező ürítése siker esetén
                    await LoadQuestions();    // Lista frissítése
                }
                else
                {
                    MessageBox.Show($"Hiba a kérdés hozzáadásakor: {await response.Content.ReadAsStringAsync()}", "Mentési Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Kritikus hiba: {ex.Message}", "Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}