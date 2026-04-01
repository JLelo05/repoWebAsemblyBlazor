using LeloPage.Pages;

namespace LeloPage.Tests.Pages;

public class ArtistPageTests : IDisposable
{
    private readonly TestContext _ctx;

    public ArtistPageTests()
    {
        _ctx = new TestContext();

        // Allow any JS call without explicit setup (localStorage.getItem/setItem, etc.)
        _ctx.JSInterop.Mode = JSRuntimeMode.Loose;

        _ctx.Services.AddSingleton(new CultureService(_ctx.JSInterop.JSRuntime));
        _ctx.Services.AddSingleton<IStringLocalizer<SharedResources>>(
            new TestStringLocalizer<SharedResources>());
    }

    public void Dispose() => _ctx.Dispose();

    // ── Calendar rendering ─────────────────────────────────────

    [Fact]
    public void Calendar_Renders_ContainsCurrentYear()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();

        var header = cut.Find(".cal-month-title");
        Assert.Contains(DateTime.Today.Year.ToString(), header.TextContent);
    }

    [Fact]
    public void Calendar_Renders_HasSevenDayHeaders()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();

        // One .cal-dow cell per day of week
        var dayNameCells = cut.FindAll(".cal-dow");
        Assert.Equal(7, dayNameCells.Count);
    }

    [Fact]
    public void Calendar_Renders_HasNavigationButtons()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();

        var navButtons = cut.FindAll(".cal-nav");
        Assert.Equal(2, navButtons.Count);
    }

    // ── Month navigation ───────────────────────────────────────

    [Fact]
    public void NextMonth_Click_ChangesHeaderToNextMonth()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        var originalText = cut.Find(".cal-month-title").TextContent;

        cut.FindAll(".cal-nav")[1].Click(); // second button = NextMonth (›)

        var newText = cut.Find(".cal-month-title").TextContent;
        Assert.NotEqual(originalText, newText);
    }

    [Fact]
    public void PrevMonth_Click_ChangesHeaderToPreviousMonth()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        var originalText = cut.Find(".cal-month-title").TextContent;

        cut.FindAll(".cal-nav")[0].Click(); // first button = PrevMonth (‹)

        var newText = cut.Find(".cal-month-title").TextContent;
        Assert.NotEqual(originalText, newText);
    }

    [Fact]
    public void NextThenPrev_Click_RestoresOriginalHeader()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        var navButtons = cut.FindAll(".cal-nav");
        var originalText = cut.Find(".cal-month-title").TextContent;

        navButtons[1].Click(); // next
        navButtons[0].Click(); // back

        Assert.Equal(originalText, cut.Find(".cal-month-title").TextContent);
    }

    // ── Day selection ──────────────────────────────────────────

    [Fact]
    public void SelectFutureDay_ShowsRegisterButton()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        NavigateToNextMonthAndSelectFirstDay(cut);

        Assert.NotNull(cut.Find(".btn-register"));
    }

    [Fact]
    public void SelectFutureDay_RegisterButtonIsVisible()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        NavigateToNextMonthAndSelectFirstDay(cut);

        // The action block appears with the selected date info
        Assert.NotNull(cut.Find(".cal-action"));
    }

    [Fact]
    public void SelectDifferentDay_UpdatesSelectedDate()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();

        cut.FindAll(".cal-nav")[1].Click(); // NextMonth — all days are now future

        // Click day 1, capture action text
        cut.FindAll(".cal-day")[0].Click();
        var firstActionText = cut.Find(".cal-action").TextContent;

        // Re-find to avoid stale event-handler references after re-render
        cut.FindAll(".cal-day")[1].Click();
        var secondActionText = cut.Find(".cal-action").TextContent;

        Assert.NotEqual(firstActionText, secondActionText);
    }

    // ── Modal open/close ───────────────────────────────────────

    [Fact]
    public void RegisterButton_Click_OpensModal()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        NavigateToNextMonthAndSelectFirstDay(cut);

        cut.Find(".btn-register").Click();

        Assert.NotNull(cut.Find(".modal-overlay"));
    }

    [Fact]
    public void ModalCancel_Click_ClosesModal()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);

        cut.Find(".btn-cancel").Click();

        Assert.Empty(cut.FindAll(".modal-overlay"));
    }

    [Fact]
    public void ModalOverlayClick_ClosesModal()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);

        cut.Find(".modal-overlay").Click();

        Assert.Empty(cut.FindAll(".modal-overlay"));
    }

    // ── Modal form fields ──────────────────────────────────────

    [Fact]
    public void Modal_Opens_WithDefaultTimeTen()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);

        var timeInput = cut.Find("#regTime");
        Assert.Equal("10:00", timeInput.GetAttribute("value"));
    }

    [Fact]
    public void Modal_Opens_WithLessonsZero()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);

        var lessonsInput = cut.Find("#regLessons");
        Assert.Equal("0", lessonsInput.GetAttribute("value"));
    }

    // ── Validation ─────────────────────────────────────────────

    [Fact]
    public void SaveRegistration_InvalidLessonCount_ShowsError()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);
        // Default: time=10:00 (valid), lessons=0 (invalid)

        cut.Find(".btn-save").Click();

        var errorEl = cut.Find(".modal-error");
        Assert.NotEmpty(errorEl.TextContent);
        // Key for lessons error returned by TestStringLocalizer
        Assert.Contains("Artist.Modal.ErrorLessons", errorEl.TextContent);
    }

    [Fact]
    public void SaveRegistration_EmptyName_ShowsError()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);

        // Provide valid lessons; leave name and phone empty
        cut.Find("#regLessons").Input("3");
        cut.Find(".btn-save").Click();

        var errorEl = cut.Find(".modal-error");
        Assert.Contains("Artist.Modal.ErrorName", errorEl.TextContent);
    }

    [Fact]
    public void SaveRegistration_EmptyPhone_ShowsError()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);

        cut.Find("#regLessons").Input("2");
        cut.Find("#regName").Input("Jan Novak");
        // Leave phone empty
        cut.Find(".btn-save").Click();

        var errorEl = cut.Find(".modal-error");
        Assert.Contains("Artist.Modal.ErrorPhone", errorEl.TextContent);
    }

    [Fact]
    public void SaveRegistration_EmptyTime_ShowsError()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);

        // Clear the time field
        cut.Find("#regTime").Input("");
        cut.Find(".btn-save").Click();

        var errorEl = cut.Find(".modal-error");
        Assert.Contains("Artist.Modal.ErrorTime", errorEl.TextContent);
    }

    [Fact]
    public void SaveRegistration_OutOfRangeTime_ShowsError()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);

        cut.Find("#regTime").Input("20:00"); // outside 08:00–17:00
        cut.Find("#regLessons").Input("2");
        cut.Find(".btn-save").Click();

        var errorEl = cut.Find(".modal-error");
        Assert.Contains("Artist.Modal.ErrorTimeRange", errorEl.TextContent);
    }

    [Fact]
    public void SaveRegistration_ValidData_ClosesModal()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);

        cut.Find("#regLessons").Input("1");
        cut.Find("#regName").Input("Jan Novak");
        cut.Find("#regPhone").Input("+421900123456");
        cut.Find(".btn-save").Click();

        Assert.Empty(cut.FindAll(".modal-overlay"));
    }

    [Fact]
    public void SaveRegistration_ValidData_AppearsInBookingsList()
    {
        var cut = _ctx.RenderComponent<ArtistPage>();
        OpenModal(cut);

        cut.Find("#regLessons").Input("1");
        cut.Find("#regName").Input("Jan Novak");
        cut.Find("#regPhone").Input("+421900123456");
        cut.Find(".btn-save").Click();

        Assert.NotNull(cut.Find(".registrations-list"));
    }

    // ── Helpers ────────────────────────────────────────────────

    /// Navigates to next month and clicks the first available day.
    private static void NavigateToNextMonthAndSelectFirstDay(IRenderedComponent<ArtistPage> cut)
    {
        cut.FindAll(".cal-nav")[1].Click(); // NextMonth
        cut.FindAll(".cal-day").First().Click();
    }

    /// Navigates to next month, selects first day, then opens the registration modal.
    private static void OpenModal(IRenderedComponent<ArtistPage> cut)
    {
        NavigateToNextMonthAndSelectFirstDay(cut);
        cut.Find(".btn-register").Click();
    }
}
