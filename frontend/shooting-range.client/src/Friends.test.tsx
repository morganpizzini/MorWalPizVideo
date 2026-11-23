import { renderToStaticMarkup } from "react-dom/server";
import { describe, expect, it, vi } from "vitest";
import { FriendList, FriendRequestList } from "./Friends";

describe("friend management list", () => {
  it("shows only explicitly connected friends and offers removal", () => {
    const html = renderToStaticMarkup(
      <FriendList
        items={[{
          id: "friend-1",
          username: "direct-friend",
          firstName: "Direct",
          lastName: "Friend",
        }]}
        busy={false}
        onRemove={vi.fn()}
      />,
    );

    expect(html).toContain("Direct Friend");
    expect(html).toContain("@direct-friend");
    expect(html).toContain("Rimuovi direct-friend");
    expect(html).not.toContain("directory");
  });

  it("communicates that the empty state means no accepted friendships", () => {
    expect(renderToStaticMarkup(
      <FriendList items={[]} busy={false} onRemove={vi.fn()} />,
    )).toContain("Non hai ancora amicizie accettate.");
  });

  it("renders every incoming request with accept and decline actions", () => {
    const requests = Array.from({ length: 4 }, (_, index) => ({
      id: `request-${index}`,
      username: `member-${index}`,
      firstName: `Member ${index}`,
      lastName: "Range",
      createdUtc: "2030-01-12T08:00:00Z",
    }));
    const html = renderToStaticMarkup(
      <FriendRequestList
        items={requests}
        direction="incoming"
        busy={false}
        onAccept={vi.fn()}
        onDecline={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    requests.forEach((request) => {
      expect(html).toContain(`@${request.username}`);
      expect(html).toContain(`Accetta la richiesta di @${request.username}`);
      expect(html).toContain(`Rifiuta la richiesta di @${request.username}`);
    });
    expect(html.match(/<time /g)).toHaveLength(requests.length);
  });

  it("offers cancellation, but no recipient decisions, for outgoing requests", () => {
    const html = renderToStaticMarkup(
      <FriendRequestList
        items={[{
          id: "request-outgoing",
          username: "waiting-member",
          firstName: "Waiting",
          lastName: "Member",
          createdUtc: "2030-01-12T08:00:00Z",
        }]}
        direction="outgoing"
        busy={false}
        onAccept={vi.fn()}
        onDecline={vi.fn()}
        onCancel={vi.fn()}
      />,
    );

    expect(html).toContain("Annulla la richiesta inviata a @waiting-member");
    expect(html).not.toContain("Accetta");
    expect(html).not.toContain("Rifiuta");
  });
});
