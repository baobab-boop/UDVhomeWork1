using System.Text;

namespace Anketa;

public partial class MainPage : ContentPage
{
    private const int RequiredCount = 3;
    private const int QuestionCount = 10;
    private const int PureDamageIndex = 2;
    private const int MaxHeroLevel = 30;
    private const int UltimateLevel = 6;
    private const int DayMinutes = 5;
    private static readonly DateTime ReleaseDate = new DateTime(2013, 7, 9);

    private const double DefaultUltLevel = 1;
    private const double DefaultDayMinutes = 1;
    private static readonly DateTime DefaultReleaseGuess = new DateTime(2020, 1, 1);

    private static readonly string[] AegisAnswers = { "aegis", "аегис", "аегида", "эгида", "эгис", "аэгис" };

    private static readonly Color DoneColor = Color.FromArgb("#2E7D32");
    private static readonly Color PendingColor = Color.FromArgb("#B5523B");

    private static readonly (int MinMmr, string Name, Color Color, string Verdict)[] Ranks =
    {
        (5420, "Титан", Color.FromArgb("#B8860B"), "Механику знаешь лучше разработчиков."),
        (4620, "Божество", Color.FromArgb("#6A1B9A"), "Почти про-сцена, осталось найти стак."),
        (3850, "Властелин", Color.FromArgb("#1565C0"), "Крепкие знания, тиммейты тебя уважают."),
        (3080, "Легенда", Color.FromArgb("#00838F"), "Выше среднего — есть чем гордиться."),
        (2310, "Герой", Color.FromArgb("#2E7D32"), "Основы знаешь, но детали ещё подтянуть."),
        (1540, "Рыцарь", Color.FromArgb("#6D4C41"), "Варды уже покупаешь, теперь бы их ставить."),
        (770, "Страж", Color.FromArgb("#5D4037"), "Пора почитать Liquipedia."),
        (0, "Рекрут", Color.FromArgb("#757575"), "Всё впереди. Главное — не ливать.")
    };

    public MainPage()
    {
        InitializeComponent();
        UpdateProgress();
    }

    private void OnRequiredTextChanged(object sender, TextChangedEventArgs e)
    {
        UpdateProgress();
    }

    private void OnDamageChanged(object sender, EventArgs e)
    {
        UpdateProgress();
    }

    private void OnHardcoreToggled(object sender, ToggledEventArgs e)
    {
        _HardcoreStack_.IsVisible = e.Value;
    }

    private async void OnShowResultClicked(object sender, EventArgs e)
    {
        var errors = new List<string>();
        if (!IsNickValid())
            errors.Add("укажите ник (не короче 2 символов)");
        if (_DamagePicker_.SelectedIndex < 0)
            errors.Add("ответьте на вопрос 1 (тип урона)");
        if (!TryGetMaxLevel(out _))
            errors.Add("ответьте на вопрос 2 целым числом");

        if (errors.Count > 0)
        {
            _ErrorLabel_.Text = "Заполните анкету до конца:\n• " + string.Join("\n• ", errors);
            _ErrorLabel_.IsVisible = true;
            _ResultBorder_.IsVisible = false;
            await _RootScroll_.ScrollToAsync(_ErrorLabel_, ScrollToPosition.Center, true);
            return;
        }

        var summary = new StringBuilder();
        double score = CheckAnswers(summary);
        bool bonus = _HardcoreSwitch_.IsToggled && _RapierDropsRadio_.IsChecked;

        int mmr = CalculateMmr(score, bonus);
        var rank = Ranks.First(r => mmr >= r.MinMmr);

        _ErrorLabel_.IsVisible = false;
        _MmrLabel_.Text = $"≈ {mmr} MMR";
        _RankLabel_.Text = $"Ранг: {rank.Name}";
        _RankLabel_.TextColor = rank.Color;
        _VerdictLabel_.Text = $"Верных ответов: {score:0.#} из {QuestionCount}" +
                              (bonus ? " + бонусный" : "") +
                              $"\n{rank.Verdict}";
        _ResultLabel_.Text = summary.ToString();
        _ResultBorder_.IsVisible = true;
        await _RootScroll_.ScrollToAsync(_ResultBorder_, ScrollToPosition.Start, true);
    }

    private async void OnClearClicked(object sender, EventArgs e)
    {
        _NickEntry_.Text = string.Empty;
        _DamagePicker_.SelectedIndex = -1;
        _MaxLevelEntry_.Text = string.Empty;
        _UltStepper_.Value = DefaultUltLevel;
        _DaySlider_.Value = DefaultDayMinutes;

        _AegisEntry_.Text = string.Empty;
        foreach (var radio in AllRadios())
            radio.IsChecked = false;

        _ShadowBladeCheck_.IsChecked = false;
        _BkbCheck_.IsChecked = false;
        _GlimmerCheck_.IsChecked = false;
        _BlinkCheck_.IsChecked = false;

        _ReleaseDatePicker_.Date = DefaultReleaseGuess;

        _DenyEditor_.Text = string.Empty;
        _HardcoreSwitch_.IsToggled = false;

        _ErrorLabel_.Text = string.Empty;
        _ErrorLabel_.IsVisible = false;
        _MmrLabel_.Text = string.Empty;
        _RankLabel_.Text = string.Empty;
        _VerdictLabel_.Text = string.Empty;
        _ResultLabel_.Text = string.Empty;
        _ResultBorder_.IsVisible = false;

        UpdateProgress();
        await _RootScroll_.ScrollToAsync(0, 0, true);
    }

    private void UpdateProgress()
    {
        int filled = 0;
        if (IsNickValid()) filled++;
        if (_DamagePicker_.SelectedIndex >= 0) filled++;
        if (TryGetMaxLevel(out _)) filled++;

        _ProgressLabel_.Text = $"Обязательные поля заполнены: {filled} из {RequiredCount}";
        _ProgressLabel_.TextColor = filled == RequiredCount ? DoneColor : PendingColor;
    }

    private bool IsNickValid()
    {
        return (_NickEntry_.Text ?? string.Empty).Trim().Length >= 2;
    }

    private bool TryGetMaxLevel(out int level)
    {
        return int.TryParse((_MaxLevelEntry_.Text ?? string.Empty).Trim(), out level);
    }

    private double CheckAnswers(StringBuilder sb)
    {
        double score = 0;
        sb.AppendLine($"Игрок: {_NickEntry_.Text.Trim()}");
        sb.AppendLine();

        score += Mark(sb, "1. Тип урона", _DamagePicker_.SelectedItem?.ToString(),
            _DamagePicker_.SelectedIndex == PureDamageIndex, "Чистый");

        TryGetMaxLevel(out int maxLevel);
        score += Mark(sb, "2. Максимальный уровень", maxLevel.ToString(),
            maxLevel == MaxHeroLevel, MaxHeroLevel.ToString());

        int ult = (int)Math.Round(_UltStepper_.Value);
        score += Mark(sb, "3. Уровень ультимейта", ult.ToString(),
            ult == UltimateLevel, UltimateLevel.ToString());

        int day = (int)Math.Round(_DaySlider_.Value);
        score += Mark(sb, "4. Длина дня", $"{day} мин",
            day == DayMinutes, $"{DayMinutes} мин");

        string aegis = (_AegisEntry_.Text ?? string.Empty).Trim().ToLowerInvariant();
        score += Mark(sb, "5. Предмет с Рошана", _AegisEntry_.Text,
            AegisAnswers.Any(a => aegis.Contains(a)), "Aegis of the Immortal");

        score += Mark(sb, "6. Стак лагеря", CheckedContent(_StackKillRadio_, _StackPullRadio_, _StackWardRadio_),
            _StackPullRadio_.IsChecked, "увести крипов на :53–:55");

        score += Mark(sb, "7. Smoke of Deceit", CheckedContent(_SmokeStunRadio_, _SmokeHealRadio_, _SmokeInvisRadio_),
            _SmokeInvisRadio_.IsChecked, "невидимость и ускорение группе");

        score += Mark(sb, "8. Иммунитет к дебаффам", CheckedContent(_BlinkRadio_, _BkbRadio_, _ScepterRadio_),
            _BkbRadio_.IsChecked, "Black King Bar");

        bool invisCorrect = _ShadowBladeCheck_.IsChecked && _GlimmerCheck_.IsChecked &&
                            !_BkbCheck_.IsChecked && !_BlinkCheck_.IsChecked;
        score += Mark(sb, "9. Предметы на невидимость", CheckedInvisItems(),
            invisCorrect, "Shadow Blade, Glimmer Cape");

        DateTime release = _ReleaseDatePicker_.Date ?? DefaultReleaseGuess;
        double releaseScore = release.Date == ReleaseDate ? 1 : release.Year == ReleaseDate.Year ? 0.5 : 0;
        sb.AppendLine($"10. Дата релиза: {release:dd.MM.yyyy} " +
                      (releaseScore == 1 ? "✓" : releaseScore > 0 ? "½ (верен только год)" : "✗") +
                      (releaseScore < 1 ? $" (верно: {ReleaseDate:dd.MM.yyyy})" : ""));
        score += releaseScore;

        if (_HardcoreSwitch_.IsToggled)
        {
            sb.AppendLine();
            Mark(sb, "Бонус. Divine Rapier", CheckedContent(_RapierStaysRadio_, _RapierDropsRadio_, _RapierGoneRadio_),
                _RapierDropsRadio_.IsChecked, "выпадает на землю");
            sb.Append($"Что такое денай: {ValueOrDash(_DenyEditor_.Text)}");
        }
        else
        {
            sb.AppendLine();
            sb.Append("Хардкорный режим: выключен");
        }

        return score;
    }

    private static int Mark(StringBuilder sb, string question, string answer, bool correct, string rightAnswer)
    {
        sb.AppendLine($"{question}: {ValueOrDash(answer)} " + (correct ? "✓" : $"✗ (верно: {rightAnswer})"));
        return correct ? 1 : 0;
    }

    private static int CalculateMmr(double score, bool bonus)
    {
        double mmr = 300 + score * 670;
        if (bonus) mmr += 500;
        return (int)Math.Round(mmr / 10) * 10;
    }

    private IEnumerable<RadioButton> AllRadios()
    {
        return new[]
        {
            _StackKillRadio_, _StackPullRadio_, _StackWardRadio_,
            _SmokeStunRadio_, _SmokeHealRadio_, _SmokeInvisRadio_,
            _BlinkRadio_, _BkbRadio_, _ScepterRadio_,
            _RapierStaysRadio_, _RapierDropsRadio_, _RapierGoneRadio_
        };
    }

    private static string CheckedContent(params RadioButton[] radios)
    {
        return radios.FirstOrDefault(r => r.IsChecked)?.Content.ToString() ?? "нет ответа";
    }

    private string CheckedInvisItems()
    {
        var items = new List<string>();
        if (_ShadowBladeCheck_.IsChecked) items.Add("Shadow Blade");
        if (_BkbCheck_.IsChecked) items.Add("Black King Bar");
        if (_GlimmerCheck_.IsChecked) items.Add("Glimmer Cape");
        if (_BlinkCheck_.IsChecked) items.Add("Blink Dagger");
        return items.Count > 0 ? string.Join(", ", items) : "ничего не отмечено";
    }

    private static string ValueOrDash(string text)
    {
        return string.IsNullOrWhiteSpace(text) ? "—" : text.Trim();
    }
}
