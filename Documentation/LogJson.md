# Log JSON

World log objects only store a `logId`. Titles, body text, and image paths are mapped in
`Assets/Resources/Data/Logs.json`.

```json
{
  "logs": [
    {
      "id": "sample-log-01",
      "title": "버려진 탐사 기록",
      "body": "로그 본문",
      "imageResource": "Logs/AbandonedBase"
    }
  ]
}
```

`imageResource` is an optional path below a Resources folder without a file extension.
For example, `Assets/Resources/Logs/AbandonedBase.png` maps to `Logs/AbandonedBase`.
IDs are case-insensitive and must be unique.
