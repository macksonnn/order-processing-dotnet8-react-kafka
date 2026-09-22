import type { ButtonHTMLAttributes } from "react";

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> & {
  variant?: "default" | "link";
};

export function Button({ variant = "default", className, type = "button", ...props }: ButtonProps) {
  const classes = [variant === "link" ? "link" : null, className].filter(Boolean).join(" ");

  return <button type={type} className={classes || undefined} {...props} />;
}
