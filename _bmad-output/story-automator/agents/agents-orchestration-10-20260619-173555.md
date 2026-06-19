---
stateFile: "/home/administrator/projects/hexalith/chatbot/_bmad-output/story-automator/orchestration-10-20260619-173555.md"
createdAt: "2026-06-19T17:36:26Z"
---

# Agents Plan: Hexalith.ChatBot - Epic Breakdown

```json
{
  "version": "1.0.0",
  "stateFile": "/home/administrator/projects/hexalith/chatbot/_bmad-output/story-automator/orchestration-10-20260619-173555.md",
  "epic": "10",
  "epicName": "Hexalith.ChatBot - Epic Breakdown",
  "createdAt": "2026-06-19T17:36:26Z",
  "stories": [
    {
      "storyId": "10.6a",
      "title": "AI-response streaming transport ADR (resolves CR-1 blocker)",
      "complexity": "low",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    },
    {
      "storyId": "10.6b",
      "title": "Streaming AI response + Stop/Cancel (UX-DR32)",
      "complexity": "low",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    },
    {
      "storyId": "12.1",
      "title": "Fluent-only + no-theme-redefinition governance guard (gates 12.2\u201312.8)",
      "complexity": "high",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    },
    {
      "storyId": "12.2",
      "title": "Migrate governed chat composer \u2192 Fluent v5",
      "complexity": "low",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    },
    {
      "storyId": "12.3",
      "title": "Migrate conversation stream + item components \u2192 Fluent v5",
      "complexity": "low",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    },
    {
      "storyId": "12.4",
      "title": "Migrate association review surface \u2192 Fluent v5",
      "complexity": "low",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    },
    {
      "storyId": "12.5",
      "title": "Migrate approval & governed-action surfaces \u2192 Fluent v5",
      "complexity": "low",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    },
    {
      "storyId": "12.6",
      "title": "Migrate policy/notification/escalation editors \u2192 Fluent v5",
      "complexity": "low",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    },
    {
      "storyId": "12.7",
      "title": "Migrate operational dashboards + compliance audit page \u2192 Fluent v5",
      "complexity": "low",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    },
    {
      "storyId": "12.8",
      "title": "Retire the `chatbot.tokens.css` custom design system",
      "complexity": "low",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    },
    {
      "storyId": "12.9",
      "title": "Cross-surface a11y / visual re-verification (re-run 10.7 against Fluent)",
      "complexity": "low",
      "tasks": {
        "create": {
          "primary": "codex",
          "fallback": "claude"
        },
        "dev": {
          "primary": "codex",
          "fallback": "claude"
        },
        "auto": {
          "primary": "codex",
          "fallback": "claude"
        },
        "review": {
          "primary": "claude",
          "fallback": "codex"
        }
      }
    }
  ]
}
```
