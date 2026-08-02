import { useMantineColorScheme } from '@mantine/core'
import type { EChartsOption } from 'echarts'
import * as echarts from 'echarts'
import { FC, useEffect, useRef } from 'react'

export interface EchartsContainerProps extends React.ComponentPropsWithoutRef<'div'> {
  option: EChartsOption
  opts?: echarts.EChartsInitOpts
  style?: React.CSSProperties
}

export const EchartsContainer: FC<EchartsContainerProps> = (props) => {
  const chartRef = useRef<HTMLDivElement>(null)
  const chartInstance = useRef<echarts.ECharts | null>(null)
  const optionRef = useRef<EChartsOption>(props.option)
  const appliedOptionRef = useRef<EChartsOption | null>(null)
  const { option, opts, style, ...rest } = props
  optionRef.current = option

  const { colorScheme } = useMantineColorScheme()
  const renderer = opts?.renderer
  const locale = opts?.locale
  const devicePixelRatio = opts?.devicePixelRatio
  const useDirtyRect = opts?.useDirtyRect
  const useCoarsePointer = opts?.useCoarsePointer
  const pointerSize = opts?.pointerSize
  const ssr = opts?.ssr
  const width = opts?.width
  const height = opts?.height

  useEffect(() => {
    if (!chartRef.current) return

    chartInstance.current = echarts.init(chartRef.current, colorScheme === 'dark' ? 'dark' : 'default', {
      renderer,
      locale,
      devicePixelRatio,
      useDirtyRect,
      useCoarsePointer,
      pointerSize,
      ssr,
      width,
      height,
    })
    chartInstance.current.setOption(optionRef.current, {
      notMerge: true,
      lazyUpdate: true,
    })
    appliedOptionRef.current = optionRef.current

    return () => {
      chartInstance.current?.dispose()
      chartInstance.current = null
      appliedOptionRef.current = null
    }
  }, [colorScheme, renderer, locale, devicePixelRatio, useDirtyRect, useCoarsePointer, pointerSize, ssr, width, height])

  useEffect(() => {
    if (appliedOptionRef.current === option) return

    chartInstance.current?.setOption(option, {
      notMerge: true,
      lazyUpdate: true,
    })
    appliedOptionRef.current = option
  }, [option])

  useEffect(() => {
    const handleResize = () => {
      if (chartInstance.current) {
        chartInstance.current.resize()
      }
    }

    window.addEventListener('resize', handleResize)

    return () => {
      window.removeEventListener('resize', handleResize)
    }
  }, [])

  return <div ref={chartRef} style={style} {...rest} />
}
