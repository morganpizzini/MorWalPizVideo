import { Fragment, useState, type ReactNode } from "react";
import { ChevronLeft, ChevronRight } from "lucide-react";
import type {
  BlogBlock,
  BlogDocument,
  BlogImage,
  BlogText,
} from "@morwalpizvideo/models";

export interface BlogRendererProps {
  document: BlogDocument;
  images: BlogImage[];
}

function safeHref(href?: string | null): string | undefined {
  if (!href || /[\\\u0000-\u0020]/.test(href)) return undefined;
  if (href.startsWith("/") && !href.startsWith("//")) return href;
  try {
    const url = new URL(href);
    return url.protocol === "https:" && !url.username && !url.password
      ? href
      : undefined;
  } catch {
    return undefined;
  }
}

function RichText({ node }: { node: BlogText }): ReactNode {
  const children = node.content?.map((child: BlogText, index: number) => (
    <RichText key={index} node={child} />
  ));
  switch (node.type) {
    case "doc":
      return <>{children}</>;
    case "paragraph":
      return <p>{children}</p>;
    case "bulletList":
      return <ul>{children}</ul>;
    case "orderedList":
      return <ol>{children}</ol>;
    case "listItem":
      return <li>{children}</li>;
    case "blockquote":
      return <blockquote>{children}</blockquote>;
    case "hardBreak":
      return <br />;
    case "text": {
      let result: ReactNode = node.text;
      for (const mark of node.marks ?? []) {
        switch (mark.type) {
          case "bold":
            result = <strong>{result}</strong>;
            break;
          case "italic":
            result = <em>{result}</em>;
            break;
          case "strike":
            result = <s>{result}</s>;
            break;
          case "code":
            result = <code>{result}</code>;
            break;
          case "link":
            result = (
              <a href={safeHref(mark.href)} rel="noopener noreferrer">
                {result}
              </a>
            );
            break;
        }
      }
      return result;
    }
    default:
      return null;
  }
}

function Picture({ image }: { image: BlogImage }) {
  return (
    <img
      src={image.publicUrl}
      alt={image.altText}
      width={image.width}
      height={image.height}
      loading="lazy"
      decoding="async"
      className="img-fluid"
      style={{ width: "100%", height: "auto" }}
    />
  );
}

function Carousel({ images }: { images: BlogImage[] }) {
  const [index, setIndex] = useState(0);
  const move = (direction: number) =>
    setIndex(
      (current) => (current + direction + images.length) % images.length,
    );
  if (!images.length) return null;
  return (
    <section
      aria-roledescription="carousel"
      aria-label="Article images"
      tabIndex={0}
      onKeyDown={(event) => {
        if (event.key === "ArrowLeft" || event.key === "ArrowRight") {
          event.preventDefault();
          move(event.key === "ArrowLeft" ? -1 : 1);
        }
      }}
    >
      <div
        style={{
          aspectRatio: "16 / 9",
          background: "#f1f1f1",
          display: "grid",
          placeItems: "center",
        }}
      >
        <img
          src={images[index % images.length].publicUrl}
          alt={images[index % images.length].altText}
          width={images[index % images.length].width}
          height={images[index % images.length].height}
          loading="lazy"
          style={{
            width: "100%",
            height: "100%",
            objectFit: "contain",
            minHeight: 0,
          }}
        />
      </div>
      <div className="d-flex align-items-center justify-content-center gap-3 mt-2">
        <button
          type="button"
          className="btn btn-outline-secondary"
          aria-label="Previous image"
          title="Previous image"
          onClick={() => move(-1)}
          disabled={images.length < 2}
        >
          <ChevronLeft size={18} aria-hidden="true" />
        </button>
        <span aria-live="polite" aria-atomic="true">
          {(index % images.length) + 1} / {images.length}
        </span>
        <button
          type="button"
          className="btn btn-outline-secondary"
          aria-label="Next image"
          title="Next image"
          onClick={() => move(1)}
          disabled={images.length < 2}
        >
          <ChevronRight size={18} aria-hidden="true" />
        </button>
      </div>
    </section>
  );
}

function Block({
  block,
  images,
}: {
  block: BlogBlock;
  images: BlogImage[];
}): ReactNode {
  const selected = (block.imageIds ?? []).flatMap((id: string) => {
    const image = images.find((item: BlogImage) => item.id === id);
    return image ? [image] : [];
  });
  switch (block.type) {
    case "richText":
      return block.richText ? <RichText node={block.richText} /> : null;
    case "heading":
      return block.level === 3 ? (
        <h3>{block.text}</h3>
      ) : block.level === 4 ? (
        <h4>{block.text}</h4>
      ) : (
        <h2>{block.text}</h2>
      );
    case "image":
      return selected[0] ? (
        <figure>
          <Picture image={selected[0]} />
          {block.text && <figcaption>{block.text}</figcaption>}
        </figure>
      ) : null;
    case "gallery":
      return (
        <div className="row g-3">
          {selected.map((image: BlogImage) => (
            <figure key={image.id} className="col-12 col-sm-6">
              <Picture image={image} />
            </figure>
          ))}
        </div>
      );
    case "carousel":
      return <Carousel images={selected} />;
    case "video":
      return /^[a-zA-Z0-9_-]{11}$/.test(block.videoId ?? "") ? (
        <div className="ratio ratio-16x9">
          <iframe
            src={`https://www.youtube-nocookie.com/embed/${block.videoId}`}
            title={block.text || "YouTube video"}
            loading="lazy"
            referrerPolicy="strict-origin-when-cross-origin"
            allow="encrypted-media; picture-in-picture"
            allowFullScreen
          />
        </div>
      ) : null;
    case "columns":
      return (
        <div className="row g-4">
          {block.columns?.map((column: BlogBlock[], index: number) => (
            <div
              key={index}
              className={`col-12 ${block.columns?.length === 3 ? "col-md-4" : block.columns?.length === 2 ? "col-md-6" : ""}`}
              style={{ minWidth: 0 }}
            >
              {column.map((child: BlogBlock) => (
                <Fragment key={child.id}>
                  <div className="mb-4">
                    <Block block={child} images={images} />
                  </div>
                </Fragment>
              ))}
            </div>
          ))}
        </div>
      );
    default:
      return null;
  }
}

export function BlogRenderer({ document, images }: BlogRendererProps) {
  if (document.version !== 1) return null;
  return (
    <div
      className="blog-document"
      style={{ overflowWrap: "anywhere", lineHeight: 1.7 }}
    >
      {document.blocks.map((block: BlogBlock) => (
        <div key={block.id} className="mb-4">
          <Block block={block} images={images} />
        </div>
      ))}
    </div>
  );
}
