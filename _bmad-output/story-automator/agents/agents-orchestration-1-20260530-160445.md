---
stateFile: "/home/administrator/projects/hexalith/chatbot/_bmad-output/story-automator/orchestration-1-20260530-160445.md"
createdAt: "2026-05-30T16:05:34Z"
---

# Agents Plan: Hexalith.ChatBot - Epic Breakdown

```json
{
  "version": "1.0.0",
  "stateFile": "/home/administrator/projects/hexalith/chatbot/_bmad-output/story-automator/orchestration-1-20260530-160445.md",
  "epic": "1",
  "epicName": "Hexalith.ChatBot - Epic Breakdown",
  "createdAt": "2026-05-30T16:05:34Z",
  "stories": [
    {
      "storyId": "1.1",
      "title": "Scaffold the buildable Hexalith.ChatBot module",
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
      "storyId": "1.2",
      "title": "Establish the OpenAPI Contract Spine, typed Client, and `IChatBotCommand`",
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
      "storyId": "1.3",
      "title": "CommandGateway admission spine with tenant binding and authorization",
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
      "storyId": "1.4",
      "title": "Fail-closed audit-commit seam with pre- and post-commit audit emission",
      "complexity": "medium",
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
      "storyId": "1.5",
      "title": "Two-altitude idempotency",
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
      "storyId": "1.6",
      "title": "Canonical lifecycle state model and transition enforcement",
      "complexity": "medium",
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
      "storyId": "1.7",
      "title": "Versioned user-safe message catalog and redaction stage",
      "complexity": "medium",
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
      "storyId": "1.8",
      "title": "Correlation propagation and long-running operation status",
      "complexity": "medium",
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
      "storyId": "1.9",
      "title": "First governed command end-to-end with surface-origin attribution",
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
      "storyId": "1.10",
      "title": "Architecture dependency fitness tests",
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
      "storyId": "1.11",
      "title": "Differential-conformance harness",
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
      "storyId": "1.12",
      "title": "Cross-tenant isolation harness",
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
      "storyId": "1.13",
      "title": "Tenant-scoped fixture and evaluation scaffold",
      "complexity": "medium",
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
      "storyId": "1.14",
      "title": "Visual inheritance and semantic token foundation",
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
      "storyId": "1.15",
      "title": "Shared governed component primitives",
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
      "storyId": "1.16",
      "title": "Interaction guardrails and streaming stop/cancel behavior",
      "complexity": "medium",
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
      "storyId": "1.17",
      "title": "Responsive and touch foundation",
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
      "storyId": "1.18",
      "title": "Accessibility and focus-management floor",
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
      "storyId": "1.19",
      "title": "Live-region and reduced-motion behavior",
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
      "storyId": "1.20",
      "title": "English/French localization infrastructure",
      "complexity": "medium",
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
      "storyId": "1.21",
      "title": "Redaction-safe off-surface affordances and recovery patterns",
      "complexity": "medium",
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
