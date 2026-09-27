import { initials } from "@/lib/format";

function hueFromName(name: string): number {
  let h = 0;
  for (let i = 0; i < name.length; i++) h = (h * 31 + name.charCodeAt(i)) % 360;
  return h;
}

interface AvatarProps {
  name: string;
  size?: "sm" | "md" | "lg";
  /** When true, derives a stable color from the name (nice for lists). */
  colorful?: boolean;
}

export function Avatar({ name, size = "md", colorful = true }: AvatarProps) {
  const cls = size === "sm" ? "avatar avatar-sm" : size === "lg" ? "avatar avatar-lg" : "avatar";
  const style = colorful
    ? (() => {
        const h = hueFromName(name);
        return {
          background: `linear-gradient(135deg, hsl(${h} 65% 60%), hsl(${(h + 38) % 360} 68% 48%))`,
        };
      })()
    : undefined;
  return (
    <div className={cls} style={style} title={name}>
      {initials(name)}
    </div>
  );
}
