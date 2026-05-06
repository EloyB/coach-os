import type { ReactNode } from "react";
import Image from "next/image";
import { cn } from "@/lib/utils";
import { Mono } from "@/components/ui/mono";
import type { ChromeVariant, ShowcaseImage } from "@/content/showcase";

interface ScreenshotFrameProps {
  chrome: ChromeVariant;
  image: ShowcaseImage;
  /** Filename hint shown in the placeholder so the right asset can be dropped in. */
  expectedFilename?: string;
  className?: string;
  priority?: boolean;
  /**
   * If provided, renders inside the frame chrome instead of the static image
   * or placeholder — used for animated mocks. The aspect-ratio container is
   * dropped so the children dictate the natural height.
   */
  children?: ReactNode;
}

export function ScreenshotFrame({
  chrome,
  image,
  expectedFilename,
  className,
  priority = false,
  children,
}: ScreenshotFrameProps) {
  if (chrome === "phone") {
    return (
      <PhoneFrame
        image={image}
        expectedFilename={expectedFilename}
        className={className}
        priority={priority}
      >
        {children}
      </PhoneFrame>
    );
  }
  return (
    <DashboardFrame
      image={image}
      expectedFilename={expectedFilename}
      className={className}
      priority={priority}
    >
      {children}
    </DashboardFrame>
  );
}

function DashboardFrame({
  image,
  expectedFilename,
  className,
  priority,
  children,
}: Omit<ScreenshotFrameProps, "chrome">) {
  return (
    <div
      className={cn(
        "relative overflow-hidden rounded-2xl border border-rule bg-white shadow-[0_30px_80px_-30px_rgba(22,21,19,0.25)]",
        className,
      )}
    >
      <div className="flex h-9 items-center gap-2 border-b border-rule bg-canvas px-4">
        <span className="h-2.5 w-2.5 rounded-full bg-rule" />
        <span className="h-2.5 w-2.5 rounded-full bg-rule" />
        <span className="h-2.5 w-2.5 rounded-full bg-rule" />
        <Mono className="ml-3 truncate text-[11px] tracking-[0.18em] text-ink-3">
          coach-os.be / dashboard
        </Mono>
      </div>
      {children ? (
        <div className="relative w-full">{children}</div>
      ) : (
        <div
          className="relative w-full"
          style={{ aspectRatio: `${image.width} / ${image.height}` }}
        >
          {image.src ? (
            <Image
              src={image.src}
              alt={image.alt}
              fill
              sizes="(min-width: 1024px) 640px, 100vw"
              className="object-cover"
              priority={priority}
            />
          ) : (
            <FramePlaceholder filename={expectedFilename} alt={image.alt} />
          )}
        </div>
      )}
    </div>
  );
}

function PhoneFrame({
  image,
  expectedFilename,
  className,
  priority,
  children,
}: Omit<ScreenshotFrameProps, "chrome">) {
  return (
    <div className={cn("relative mx-auto w-full max-w-[280px]", className)}>
      <div className="relative overflow-hidden rounded-[2.5rem] border-[10px] border-ink bg-ink shadow-[0_40px_80px_-30px_rgba(22,21,19,0.45)]">
        <div className="absolute left-1/2 top-2 z-10 h-5 w-24 -translate-x-1/2 rounded-full bg-ink" />
        {children ? (
          <div className="relative w-full bg-canvas">{children}</div>
        ) : (
          <div
            className="relative w-full bg-canvas"
            style={{ aspectRatio: `${image.width} / ${image.height}` }}
          >
            {image.src ? (
              <Image
                src={image.src}
                alt={image.alt}
                fill
                sizes="280px"
                className="object-cover"
                priority={priority}
              />
            ) : (
              <FramePlaceholder filename={expectedFilename} alt={image.alt} />
            )}
          </div>
        )}
      </div>
    </div>
  );
}

function FramePlaceholder({
  filename,
  alt,
}: {
  filename?: string;
  alt: string;
}) {
  return (
    <div
      role="img"
      aria-label={alt}
      className="absolute inset-0 flex flex-col items-center justify-center gap-3 bg-[repeating-linear-gradient(135deg,_var(--color-canvas)_0_8px,_var(--color-paper)_8px_16px)] p-6 text-center"
    >
      <Mono className="text-[10px] tracking-[0.2em] text-ink-3">
        SCREENSHOT
      </Mono>
      {filename ? (
        <Mono className="break-all rounded-md border border-rule bg-paper px-2.5 py-1 text-[11px] tracking-tight text-ink-2">
          {filename}
        </Mono>
      ) : null}
      <p className="max-w-[28ch] text-xs leading-snug text-ink-3">{alt}</p>
    </div>
  );
}
