import {
  Box,
  Card,
  Center,
  Code,
  Divider,
  Group,
  Stack,
  Text,
  Title,
  Tooltip,
  alpha,
  useMantineTheme,
} from '@mantine/core'
import { mdiFlag } from '@mdi/js'
import { Icon } from '@mdi/react'
import cx from 'clsx'
import dayjs from 'dayjs'
import { FC, useMemo } from 'react'
import { Trans } from 'react-i18next'
import { ScrollingText } from '@Components/ScrollingText'
import { useLanguage } from '@Utils/I18n'
import { BloodsTypes, PartialIconProps, useChallengeCategoryLabelMap } from '@Utils/Shared'
import { ChallengeInfo, SubmissionType } from '@Api'
import classes from '@Styles/ChallengeCard.module.css'
import misc from '@Styles/Misc.module.css'

interface ChallengeCardProps {
  challenge: ChallengeInfo
  solved?: boolean
  onClick?: () => void
  iconMap: Map<SubmissionType, PartialIconProps | undefined>
  colorMap: Map<SubmissionType, string | undefined>
  teamId?: number
}

export const ChallengeCard: FC<ChallengeCardProps> = (props: ChallengeCardProps) => {
  const { challenge, solved, onClick, iconMap, teamId, colorMap } = props
  const challengeCategoryLabelMap = useChallengeCategoryLabelMap()
  const cateData = challengeCategoryLabelMap.get(challenge.category!)
  const theme = useMantineTheme()
  const { locale } = useLanguage()

  const isFaded = useMemo(() => {
    if (!challenge.deadline) return false

    return dayjs().isAfter(dayjs(challenge.deadline))
  }, [challenge.deadline])

  return (
    <Card
      onClick={onClick}
      shadow="sm"
      className={cx(misc.hoverCard, classes.root)}
      data-faded={solved || isFaded || undefined}
      data-no-move
    >
      <Stack gap="xs" pos="relative" className={classes.content}>
        <Group className={classes.titleRow} wrap="nowrap" justify="space-between" gap={2}>
          <ScrollingText text={challenge.title || ''} size="lg" />
        </Group>
        <Divider size="sm" color={cateData?.color} />
        <Group className={classes.metaRow} wrap="nowrap" justify="space-between" align="flex-start" gap="xs">
          <Text className={classes.score} fw="bold" fz="lg" ff="monospace">
            {challenge.score}&nbsp;pts
          </Text>
          <Stack className={classes.solveBox} gap={4}>
            <Title order={6} className={classes.solveText}>
              <Trans
                i18nKey={'challenge.content.solved'}
                values={{
                  solved: challenge.solved,
                }}
              >
                _
                <Code fz="sm" fw="bolder" bg="transparent">
                  _
                </Code>
                _
              </Trans>
            </Title>
            <Group justify="center" gap="md" h={20} wrap="nowrap">
              {challenge.bloods &&
                challenge.bloods.map((blood, idx) => {
                  const iconProps = iconMap.get(BloodsTypes[idx])!
                  return (
                    <Tooltip.Floating
                      key={idx}
                      position="bottom"
                      multiline
                      label={
                        <Stack gap={0}>
                          <Text fw={500} size="sm">
                            {blood?.name}
                          </Text>
                          <Text fw={500} size="xs" c="dimmed">
                            {dayjs(blood?.submitTimeUtc).locale(locale).format('SLL LTS')}
                          </Text>
                        </Stack>
                      }
                    >
                      <div style={{ position: 'relative', height: 20 }}>
                        <div className={classes.blood}>
                          <Icon {...iconProps} />
                        </div>
                        <Box
                          className={classes.spike}
                          data-blood={teamId === blood?.id || undefined}
                          __vars={{
                            '--blood-color': colorMap.get(BloodsTypes[idx]),
                          }}
                        />
                      </div>
                    </Tooltip.Floating>
                  )
                })}
            </Group>
          </Stack>
        </Group>
      </Stack>
      {cateData && (
        <Icon
          size={4}
          path={cateData.icon}
          color={alpha(theme.colors[cateData?.color][7], 0.3)}
          className={classes.icon}
        />
      )}
      {solved && (
        <Center className={classes.flag}>
          <Icon size={1} path={mdiFlag} />
        </Center>
      )}
    </Card>
  )
}
