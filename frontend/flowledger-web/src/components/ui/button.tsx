import { Slot } from '@radix-ui/react-slot'
import { cva, type VariantProps } from 'class-variance-authority'
import type { ButtonHTMLAttributes } from 'react'
import { cn } from '../../lib/utils'

const buttonVariants = cva(
  'inline-flex h-10 items-center justify-center gap-2 whitespace-nowrap rounded-md text-sm font-semibold transition-colors focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-900 disabled:pointer-events-none disabled:opacity-50',
  {
    variants: {
      variant: {
        default: 'bg-slate-950 text-white hover:bg-slate-800',
        secondary: 'bg-blue-950 text-white hover:bg-blue-900',
        accent: 'bg-yellow-600 text-slate-950 hover:bg-yellow-500',
        destructive: 'bg-red-700 text-white hover:bg-red-800',
        outline: 'border border-slate-300 bg-white text-slate-950 hover:bg-slate-100',
        ghost: 'text-slate-700 hover:bg-slate-100 hover:text-slate-950',
      },
      size: {
        default: 'px-4 py-2',
        sm: 'h-9 px-3',
        icon: 'h-10 w-10',
      },
    },
    defaultVariants: {
      variant: 'default',
      size: 'default',
    },
  },
)

type ButtonProps = ButtonHTMLAttributes<HTMLButtonElement> &
  VariantProps<typeof buttonVariants> & {
    asChild?: boolean
  }

export function Button({ className, variant, size, asChild = false, ...props }: ButtonProps) {
  const Comp = asChild ? Slot : 'button'
  return <Comp className={cn(buttonVariants({ variant, size, className }))} {...props} />
}
