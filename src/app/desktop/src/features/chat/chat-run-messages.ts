import type {
    ChatMessage,
    ChatRunSnapshot,
} from "../../shared/network/contracts";

export const applyChatRunSnapshotToMessages = (
    messages: ChatMessage[],
    snapshot: ChatRunSnapshot,
): ChatMessage[] =>
    messages.map((item, index) =>
        index === messages.length - 1 && item.role === "assistant"
            ? {
                  ...item,
                  content: snapshot.content,
                  skillActivities: snapshot.skillActivities,
                  runStatus: snapshot.status,
                  usage:
                      snapshot.status === "completed"
                          ? snapshot.usage
                          : undefined,
              }
            : item,
    );
