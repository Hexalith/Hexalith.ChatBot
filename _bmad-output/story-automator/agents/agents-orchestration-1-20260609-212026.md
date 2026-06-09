---
stateFile: "/home/administrator/projects/hexalith/chatbot/_bmad-output/story-automator/orchestration-1-20260609-212026.md"
createdAt: "2026-06-09T21:21:23Z"
---

# Agents Plan: Hexalith.ChatBot - Epic Breakdown

```json
{
  "version": "1.0.0",
  "stateFile": "/home/administrator/projects/hexalith/chatbot/_bmad-output/story-automator/orchestration-1-20260609-212026.md",
  "epic": "1",
  "epicName": "Hexalith.ChatBot - Epic Breakdown",
  "createdAt": "2026-06-09T21:21:23Z",
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
    },
    {
      "storyId": "2.1",
      "title": "Microsoft 365 mailbox intake and source-identity capture",
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
      "storyId": "2.2",
      "title": "Participant resolution and unresolved/unauthorized handling",
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
      "storyId": "2.3",
      "title": "Deterministic association scorer and candidate generation",
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
      "storyId": "2.4",
      "title": "Ambiguous-association detection and fail-closed routing",
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
      "storyId": "2.5",
      "title": "Ambiguous association review surface (S2)",
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
      "storyId": "2.6",
      "title": "Association decision recording, evidence preservation, and notes",
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
      "storyId": "2.7",
      "title": "Association correction and supersession",
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
      "storyId": "2.8",
      "title": "Correction propagation contract",
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
      "storyId": "2.9",
      "title": "Duplicate detection, retry, and failure states",
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
      "storyId": "3.1",
      "title": "Render email-derived project conversation (S1)",
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
      "storyId": "3.2",
      "title": "Associated-email rendering in the conversation stream",
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
      "storyId": "3.3",
      "title": "Participant rendering in the conversation stream",
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
      "storyId": "3.4",
      "title": "Attachment rendering in the conversation stream",
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
      "storyId": "3.5",
      "title": "Association and correction decision rendering",
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
      "storyId": "3.6",
      "title": "Approval event rendering",
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
      "storyId": "3.7",
      "title": "Failure, retry, and blocked-state rendering",
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
      "storyId": "3.8",
      "title": "AI outcome rendering",
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
      "storyId": "3.9",
      "title": "\"Why this project\" evidence and provenance panel",
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
      "storyId": "3.10",
      "title": "Conversation item status and next action",
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
      "storyId": "3.11",
      "title": "Informational/actionable classification, AI-summary distinction, and review history",
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
      "storyId": "3.12",
      "title": "Attachment capture and governed-folder storage",
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
      "storyId": "3.13",
      "title": "Attachment status, states, and authorization",
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
      "storyId": "3.14",
      "title": "Scoped AI-context packaging from authorized files",
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
      "storyId": "4.1",
      "title": "Task-intent detection and data contract",
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
      "storyId": "4.2",
      "title": "Task-intent review, conversion, and disposition",
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
      "storyId": "4.3",
      "title": "AI action risk classification",
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
      "storyId": "4.4",
      "title": "Low-risk AI assistance execution",
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
      "storyId": "4.5",
      "title": "Approval gate and AI action approval surface (S3)",
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
      "storyId": "4.6",
      "title": "AI action preview and inspection",
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
      "storyId": "4.7",
      "title": "Allowlisted command execution",
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
      "storyId": "4.8",
      "title": "Refusal and safe-block behavior",
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
      "storyId": "4.9",
      "title": "Correction invalidates AI action proposals",
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
      "storyId": "5.1",
      "title": "Service-client identities and scoped grants",
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
      "storyId": "5.2",
      "title": "CLI adapter and workflow parity",
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
      "storyId": "5.3",
      "title": "MCP adapter and governed tool surface",
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
      "storyId": "5.4",
      "title": "Cross-surface equivalence verification",
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
      "storyId": "6.1",
      "title": "Sender-authority classes and M365 mapping",
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
      "storyId": "6.2",
      "title": "Outbound draft creation within authority",
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
      "storyId": "6.3",
      "title": "Outbound approval gate and approval record",
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
      "storyId": "6.4",
      "title": "Inbound authenticity passthrough and header inspection",
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
      "storyId": "6.5",
      "title": "On-behalf-of disambiguation and external-sender posture",
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
      "storyId": "7.1",
      "title": "Tenant-admin permission model and bounded scopes",
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
      "storyId": "7.2",
      "title": "Policy-admin scope, Tenant Policy Schema editor, and AI action policy",
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
      "storyId": "7.3",
      "title": "Mailbox-admin scope and mailbox configuration",
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
      "storyId": "7.4",
      "title": "Compliance-admin scope",
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
      "storyId": "7.5",
      "title": "Operational queue management",
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
      "storyId": "7.6",
      "title": "Notification routing and delivery",
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
      "storyId": "7.7",
      "title": "Escalation policy for unresolved states",
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
      "storyId": "7.8",
      "title": "Approval queue prioritization and grouping",
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
      "storyId": "7.9",
      "title": "Notification throttling and digest rollup",
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
      "storyId": "7.10",
      "title": "Reviewer backlog alerting",
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
      "storyId": "7.11",
      "title": "Rubber-stamp-rate observable",
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
      "storyId": "7.12",
      "title": "Disable mailbox source",
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
      "storyId": "7.13",
      "title": "Quarantine mailbox source",
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
      "storyId": "7.14",
      "title": "Rate-limit mailbox source",
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
      "storyId": "7.15",
      "title": "Disable service client",
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
      "storyId": "7.16",
      "title": "Quarantine service client",
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
      "storyId": "7.17",
      "title": "Rate-limit service client",
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
      "storyId": "7.18",
      "title": "Disable AI actor",
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
      "storyId": "7.19",
      "title": "Quarantine AI actor",
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
      "storyId": "7.20",
      "title": "Rate-limit AI actor",
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
      "storyId": "7.21",
      "title": "Disable command capability",
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
      "storyId": "7.22",
      "title": "Quarantine command capability",
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
      "storyId": "7.23",
      "title": "Rate-limit command capability",
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
      "storyId": "7.24",
      "title": "Disable outbound channel",
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
      "storyId": "7.25",
      "title": "Quarantine outbound channel",
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
      "storyId": "7.26",
      "title": "Rate-limit outbound channel",
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
      "storyId": "7.27",
      "title": "Command allowlist v1 and full lifecycle completion",
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
      "storyId": "8.1",
      "title": "Operational dashboards (S8/S10)",
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
      "storyId": "8.2",
      "title": "Operational telemetry emission",
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
      "storyId": "8.3",
      "title": "SLO publication and error budgets",
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
      "storyId": "8.4",
      "title": "Tenant-safe alert wiring",
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
      "storyId": "8.5",
      "title": "Degraded-state operability and runbook diagnostics",
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
      "storyId": "8.6",
      "title": "Hosted Dapr Workflow production binding and saga readiness validation",
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
      "storyId": "8.7",
      "title": "Control-plane runtime activation \u2014 durable control-state/rate-limit projection and periodic enforcement trigger",
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
      "storyId": "9.1",
      "title": "Tamper-evident WORM audit chain",
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
      "storyId": "9.2",
      "title": "Audit completeness as a production observable",
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
      "storyId": "9.3",
      "title": "Audit query and compliance investigation surface (S9)",
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
      "storyId": "9.4",
      "title": "Replay and simulation isolation",
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
      "storyId": "9.5",
      "title": "Derived-store cross-tenant isolation",
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
      "storyId": "9.6",
      "title": "Correction-driven vector reindexing",
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
      "storyId": "9.7",
      "title": "Data-class inventory and retention policy",
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
      "storyId": "9.8",
      "title": "Tenant export workflow",
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
      "storyId": "9.9",
      "title": "Deletion and erasure workflow",
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
      "storyId": "9.10",
      "title": "Consent and lawful-basis metadata",
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
      "storyId": "9.11",
      "title": "Continuity drill and RPO/RTO validation",
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
      "storyId": "9.12",
      "title": "Projection rebuild validation",
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
      "storyId": "9.13",
      "title": "Scoped outage degradation validation",
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
      "storyId": "10.1",
      "title": "FrontComposer Shell integration (closes Story 1.14 deferred shell swap)",
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
      "storyId": "10.2",
      "title": "Migrate M0 governed surfaces (S1/S2/S3) onto the shell",
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
      "storyId": "10.3",
      "title": "Migrate operational surfaces (S8 dashboards, S9 audit, S10 admin queues) onto the shell",
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
      "storyId": "10.4",
      "title": "Project Workspace landing route (UX-DR5)",
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
      "storyId": "10.5",
      "title": "Governed chat composer (UX-DR16, UX-DR17)",
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
      "storyId": "10.7",
      "title": "Cross-surface a11y / visual / parity re-verification",
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
      "storyId": "11.1",
      "title": "Host-reuse ADR \u2014 DomainService SDK adoption decision record",
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
      "storyId": "11.2",
      "title": "Platform pre-commit admission hook in the DomainService SDK",
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
      "storyId": "11.3",
      "title": "Migrate ChatBot query endpoints to `IDomainQueryHandler` + `IQueryCursorCodec`",
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
      "storyId": "11.4",
      "title": "Migrate projections, telemetry, and health to SDK contracts",
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
      "storyId": "11.5",
      "title": "Reduce the Server host to the SDK shape with the CommandGateway admission hook",
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
      "storyId": "11.6",
      "title": "Retire module-owned `AppHost`/`Aspire`/`ServiceDefaults`; compose via `AddEventStoreDomainModule`",
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
    }
  ]
}
```
