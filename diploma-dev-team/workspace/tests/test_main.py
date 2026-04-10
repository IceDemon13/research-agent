from src.main import build_feature_summary, main


def test_main_returns_non_empty_message():
    result = main()
    assert isinstance(result, str)
    assert result


def test_feature_summary_contains_expected_title():
    summary = build_feature_summary()
    assert summary["title"] == "CLI Todo App Implementation"
