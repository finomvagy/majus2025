using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Newtonsoft.Json;
// Microsoft.VisualBasic referenciát adj hozzá a projekthez az InputBox használatához
// Jobb klikk a projekten -> Add -> Reference... -> Assemblies -> Framework -> Microsoft.VisualBasic
using Microsoft.VisualBasic;

namespace majus2025
{
    public partial class MainWindow : Window
    {
        private const string BaseUrl = "http://localhost:5555";
        private string jwtToken = null;
        private HttpClient httpClient = new HttpClient(); // HttpClient példányosítása egyszer

        public class Quiz
        {
            public int id { get; set; }
            public string title { get; set; }
            public string description { get; set; }
            public string difficulty { get; set; }
            public string category { get; set; }
            public double? averageScore { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            // Kezdetben a kvízkezelő rész legyen kevésbé látható vagy letiltott, amíg nincs bejelentkezés
            // Most csak a lista ürítését végezzük, ha nincs token
            UpdateQuizListVisibility();
        }

        private void UpdateQuizListVisibility()
        {
            bool isLoggedIn = !string.IsNullOrEmpty(jwtToken);
            // Itt lehetne a kvízkezelő Border láthatóságát vagy engedélyezettségét állítani
            // Pl. QuizManagementBorder.IsEnabled = isLoggedIn;
            // Jelenleg csak a lista tartalmát befolyásolja
            if (!isLoggedIn)
            {
                spQuizzes.Children.Clear();
                tbLoginMessage.Text = "Kvízek megtekintéséhez és kezeléséhez jelentkezzen be.";
                tbLoginMessage.Foreground = Brushes.OrangeRed;
            }
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            tbLoginMessage.Text = "";
            if (string.IsNullOrWhiteSpace(tbUsername.Text) || string.IsNullOrWhiteSpace(pbPassword.Password))
            {
                tbLoginMessage.Foreground = Brushes.LightCoral;
                tbLoginMessage.Text = "Töltsd ki a felhasználónevet és a jelszót!";
                return;
            }

            var userData = new { username = tbUsername.Text, password = pbPassword.Password };
            try
            {
                string jsonData = JsonConvert.SerializeObject(userData);
                StringContent content = new(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"{BaseUrl}/auth/login", content);

                if (response.IsSuccessStatusCode)
                {
                    string jsonResp = await response.Content.ReadAsStringAsync();
                    dynamic respObj = JsonConvert.DeserializeObject(jsonResp);
                    jwtToken = respObj.token;
                    httpClient.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", jwtToken);

                    tbLoginMessage.Foreground = Brushes.LightGreen;
                    tbLoginMessage.Text = "Sikeres bejelentkezés!";
                    UpdateQuizListVisibility();
                    await FetchAndDisplayQuizzes("/quizzes"); // Alapértelmezett lista betöltése
                }
                else
                {
                    string errMsg = await response.Content.ReadAsStringAsync();
                    dynamic errObj = JsonConvert.DeserializeObject(errMsg);
                    tbLoginMessage.Foreground = Brushes.LightCoral;
                    tbLoginMessage.Text = $"Bejelentkezési hiba: {errObj?.message ?? errMsg}";
                }
            }
            catch (Exception ex)
            {
                tbLoginMessage.Foreground = Brushes.LightCoral;
                tbLoginMessage.Text = "Hálózati vagy egyéb hiba: " + ex.Message;
            }
        }

        private async void BtnRegister_Click(object sender, EventArgs e)
        {
            tbLoginMessage.Text = "";
            if (string.IsNullOrWhiteSpace(tbUsername.Text) || string.IsNullOrWhiteSpace(pbPassword.Password))
            {
                tbLoginMessage.Foreground = Brushes.LightCoral;
                tbLoginMessage.Text = "Töltsd ki a felhasználónevet és a jelszót!";
                return;
            }
            var userData = new { username = tbUsername.Text, password = pbPassword.Password };
            try
            {
                string jsonData = JsonConvert.SerializeObject(userData);
                StringContent content = new(jsonData, Encoding.UTF8, "application/json");
                HttpResponseMessage response = await httpClient.PostAsync($"{BaseUrl}/users/register", content);
                string respContent = await response.Content.ReadAsStringAsync();
                dynamic respObj = JsonConvert.DeserializeObject(respContent);

                if (response.IsSuccessStatusCode)
                {
                    tbLoginMessage.Foreground = Brushes.LightGreen;
                    tbLoginMessage.Text = respObj?.message ?? "Sikeres regisztráció!";
                }
                else
                {
                    tbLoginMessage.Foreground = Brushes.LightCoral;
                    tbLoginMessage.Text = $"Regisztrációs hiba: {respObj?.message ?? respContent}";
                }
            }
            catch (Exception ex)
            {
                tbLoginMessage.Foreground = Brushes.LightCoral;
                tbLoginMessage.Text = "Hálózati vagy egyéb hiba: " + ex.Message;
            }
        }

        private async Task FetchAndDisplayQuizzes(string relativeUrl)
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                UpdateQuizListVisibility();
                return;
            }

            try
            {
                HttpResponseMessage response = await httpClient.GetAsync($"{BaseUrl}{relativeUrl}");
                if (response.IsSuccessStatusCode)
                {
                    string json = await response.Content.ReadAsStringAsync();
                    List<Quiz> quizzes = JsonConvert.DeserializeObject<List<Quiz>>(json);
                    spQuizzes.Children.Clear();

                    if (quizzes == null || quizzes.Count == 0)
                    {
                        TextBlock tbEmpty = new TextBlock()
                        {
                            Text = "Nincsenek a feltételeknek megfelelő kvízek.",
                            Foreground = Brushes.LightGray,
                            HorizontalAlignment = HorizontalAlignment.Center,
                            Margin = new Thickness(10)
                        };
                        spQuizzes.Children.Add(tbEmpty);
                        return;
                    }

                    foreach (var quiz in quizzes)
                    {
                        Border border = new Border()
                        {
                            Background = Brushes.DimGray,
                            CornerRadius = new CornerRadius(8),
                            Margin = new Thickness(5),
                            Padding = new Thickness(10),
                        };
                        StackPanel sp = new StackPanel();

                        TextBlock tbTitle = new TextBlock() { Text = $"Cím: {quiz.title}", Foreground = Brushes.White, FontWeight = FontWeights.Bold, FontSize = 14 };
                        TextBlock tbDesc = new TextBlock() { Text = $"Leírás: {quiz.description}", Foreground = Brushes.LightGray, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 2) };
                        TextBlock tbDifficulty = new TextBlock() { Text = $"Nehézség: {quiz.difficulty ?? "N/A"}", Foreground = Brushes.LightGray, Margin = new Thickness(0, 2, 0, 2) };
                        TextBlock tbCategory = new TextBlock() { Text = $"Kategória: {quiz.category ?? "N/A"}", Foreground = Brushes.LightGray, Margin = new Thickness(0, 2, 0, 5) };
                        TextBlock tbAvgScore = new TextBlock() { Text = quiz.averageScore.HasValue ? $"Átlagpontszám: {quiz.averageScore.Value:F2}" : "Átlagpontszám: -", Foreground = Brushes.LightCyan, Margin = new Thickness(0, 0, 0, 5) };


                        Button btnDetails = new Button() { Content = "Részletek", Width = 70, Margin = new Thickness(0, 0, 5, 0), Tag = quiz };
                        Button btnEdit = new Button() { Content = "Szerkesztés", Width = 80, Margin = new Thickness(0, 0, 5, 0), Tag = quiz };
                        Button btnDelete = new Button() { Content = "Törlés", Width = 70, Margin = new Thickness(0, 0, 5, 0), Tag = quiz.id };
                        Button questionWindowBtn = new Button() { Content = "Kérdések", Width = 70, Tag = quiz.id };

                        btnDetails.Click += BtnShowDetails_Click;
                        btnEdit.Click += BtnInitEditQuiz_Click;
                        btnDelete.Click += BtnDeleteQuiz_Click;
                        questionWindowBtn.Click += BtnOpenQuestionsWindow_Click;

                        StackPanel spButtons = new StackPanel() { Orientation = Orientation.Horizontal };
                        spButtons.Children.Add(btnDetails);
                        spButtons.Children.Add(btnEdit);
                        spButtons.Children.Add(btnDelete);
                        spButtons.Children.Add(questionWindowBtn);

                        sp.Children.Add(tbTitle);
                        sp.Children.Add(tbDesc);
                        sp.Children.Add(tbDifficulty);
                        sp.Children.Add(tbCategory);
                        if (quiz.averageScore.HasValue) sp.Children.Add(tbAvgScore); // Csak akkor jelenítjük meg, ha van értéke
                        sp.Children.Add(spButtons);
                        border.Child = sp;
                        spQuizzes.Children.Add(border);
                    }
                }
                else
                {
                    string errorMsg = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Hiba a kvízek lekérésekor ({response.StatusCode}): {errorMsg}", "Lekérési Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                    spQuizzes.Children.Clear(); // Hiba esetén is ürítsük a listát
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba: " + ex.Message, "Kritikus Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                spQuizzes.Children.Clear();
            }
        }

        private void BtnShowDetails_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Quiz quiz)
            {
                MessageBox.Show($"Kvíz részletei:\n\nCím: {quiz.title}\nLeírás: {quiz.description}\nNehézség: {quiz.difficulty ?? "N/A"}\nKategória: {quiz.category ?? "N/A"}", "Kvíz részletek");
            }
        }

        private void BtnInitEditQuiz_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is Quiz quiz)
            {
                tbQuizTitle.Text = quiz.title;
                tbQuizDescription.Text = quiz.description;
                tbQuizDifficulty.Text = quiz.difficulty ?? "";
                tbQuizCategory.Text = quiz.category ?? "";
                tbQuizEditId.Text = quiz.id.ToString(); // Store ID for saving
                btnSaveQuiz.Content = "Módosítások mentése";
            }
        }

        private async void BtnDeleteQuiz_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int quizId)
            {
                if (MessageBox.Show("Biztos törlöd a kvízt?", "Megerősítés", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    try
                    {
                        HttpResponseMessage delResp = await httpClient.DeleteAsync($"{BaseUrl}/quizzes/{quizId}");
                        if (delResp.IsSuccessStatusCode)
                        {
                            MessageBox.Show("Kvíz törölve.", "Siker", MessageBoxButton.OK, MessageBoxImage.Information);
                            await FetchAndDisplayQuizzes("/quizzes"); // Frissítjük a listát
                        }
                        else
                        {
                            MessageBox.Show($"Hiba a törléskor: {await delResp.Content.ReadAsStringAsync()}", "Törlési Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Hiba: " + ex.Message, "Kritikus Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnOpenQuestionsWindow_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int quizId)
            {
                var questionsWindow = new QuestionsWindow(quizId, jwtToken);
                // this.Hide(); // Vagy IsEnabled = false;
                questionsWindow.Owner = this; // Jobb, ha a főablak a tulajdonos
                questionsWindow.ShowDialog(); // Modális megjelenítés
                // this.Show(); // Vagy IsEnabled = true;
            }
        }


        private async void BtnCreateQuiz_Click(object sender, RoutedEventArgs e) // Most már mentésként is funkcionál
        {
            string title = tbQuizTitle.Text.Trim();
            string description = tbQuizDescription.Text.Trim();
            string difficulty = tbQuizDifficulty.Text.Trim();
            string category = tbQuizCategory.Text.Trim();

            if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(description))
            {
                MessageBox.Show("A cím és a leírás kitöltése kötelező!", "Hiányzó Adatok", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var quizData = new
            {
                title,
                description,
                difficulty = string.IsNullOrWhiteSpace(difficulty) ? null : difficulty,
                category = string.IsNullOrWhiteSpace(category) ? null : category
            };
            string jsonData = JsonConvert.SerializeObject(quizData);
            StringContent content = new(jsonData, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response;
                bool isEditMode = !string.IsNullOrWhiteSpace(tbQuizEditId.Text);

                if (isEditMode && int.TryParse(tbQuizEditId.Text, out int quizIdToEdit))
                {
                    response = await httpClient.PutAsync($"{BaseUrl}/quizzes/{quizIdToEdit}", content);
                }
                else
                {
                    response = await httpClient.PostAsync($"{BaseUrl}/quizzes", content);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show(isEditMode ? "Kvíz sikeresen frissítve!" : "Kvíz sikeresen létrehozva!", "Siker", MessageBoxButton.OK, MessageBoxImage.Information);
                    tbQuizTitle.Text = "";
                    tbQuizDescription.Text = "";
                    tbQuizDifficulty.Text = "";
                    tbQuizCategory.Text = "";
                    tbQuizEditId.Text = ""; // Clear edit ID
                    btnSaveQuiz.Content = "Új kvíz mentése"; // Reset button text
                    await FetchAndDisplayQuizzes("/quizzes");
                }
                else
                {
                    MessageBox.Show($"Hiba a kvíz mentésekor: {await response.Content.ReadAsStringAsync()}", "Mentési Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Hiba: " + ex.Message, "Kritikus Hiba", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            string searchTerm = tbSearchTitle.Text.Trim();
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                MessageBox.Show("Kérem, adja meg a keresési kifejezést a címhez!", "Keresés", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await FetchAndDisplayQuizzes($"/quizzes?title={Uri.EscapeDataString(searchTerm)}");
        }

        private async void BtnFilterByDifficulty_Click(object sender, RoutedEventArgs e)
        {
            string difficulty = tbFilterDifficulty.Text.Trim();
            if (string.IsNullOrWhiteSpace(difficulty))
            {
                MessageBox.Show("Kérem, adja meg a nehézségi szintet a szűréshez!", "Szűrés", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await FetchAndDisplayQuizzes($"/quizzes/difficulty/{Uri.EscapeDataString(difficulty)}");
        }

        private async void BtnFilterByCategory_Click(object sender, RoutedEventArgs e)
        {
            string category = tbFilterCategory.Text.Trim();
            if (string.IsNullOrWhiteSpace(category))
            {
                MessageBox.Show("Kérem, adja meg a kategóriát a szűréshez!", "Szűrés", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            await FetchAndDisplayQuizzes($"/quizzes/category/{Uri.EscapeDataString(category)}");
        }

        private async void BtnSortByAverageScore_Click(object sender, RoutedEventArgs e)
        {
            await FetchAndDisplayQuizzes("/quizzes/sort/average_score");
        }

        private async void BtnResetFiltersAndSort_Click(object sender, RoutedEventArgs e)
        {
            tbSearchTitle.Text = "";
            tbFilterDifficulty.Text = "";
            tbFilterCategory.Text = "";
            await FetchAndDisplayQuizzes("/quizzes");
        }
    }
}